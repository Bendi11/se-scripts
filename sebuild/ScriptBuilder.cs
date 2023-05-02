using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Build.Construction;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;
using System.Diagnostics;

namespace SeBuild;

using MSBuildProject = Microsoft.Build.Evaluation.Project;
using MSBuildProjectCollection = Microsoft.Build.Evaluation.ProjectCollection;

public class ScriptBuilder: IDisposable {
    ScriptCommon Common;
    readonly MSBuildWorkspace workspace;
    string scriptDir;
    public string GameScriptDir {
        get => scriptDir;
    }

    public ulong InitialChars = 0;

    DeadCodeRemover DeadCodePass;
    Renamer RenamePass;

    static ScriptBuilder() { MSBuildLocator.RegisterDefaults(); }
    
    /// <summary>Create a new <c>ScriptWorkspaceContext</c></summary>
    /// <param name="path">
    /// If <c>path</c> is a file, then attempt to open a solution from the given file
    /// Otherwise, search through the directory that <c>path</c> points to to find a .sln file
    /// </param>
    static async public Task<ScriptBuilder> Create(BuildArgs args) {
        var me = new ScriptBuilder();
        var project = await me.Init(args.SolutionPath, args.Project);

        /*var projectId = (
            sln
                .Projects
                .SingleOrDefault(p => p.Name == args.Project)?? throw new Exception($"Solution does not contain a project with name ${args.Project}")
        ).Id;*/

        
        me.Common = new ScriptCommon(project.Solution, project.Id, new List<DocumentId>(), args);
         
        me.GetDocuments(project.Id, new HashSet<ProjectId>());
        me.DeadCodePass = new DeadCodeRemover(me.Common);
        me.RenamePass = new Renamer(me.Common);
        return me;
    }

    public void Dispose() {
        workspace.Dispose();
    }

    /// <summary>Build the given <c>Project</c> and return a list of declaration <c>CSharpSyntaxNode</c>s</summary>
    async public Task<List<CSharpSyntaxNode>> BuildProject() {
        //Collect diagnostics before renaming identifiers
        var diags = new List<Diagnostic>();//(await Common.Project.GetCompilationAsync())!.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning);

        if(Common.Args.RemoveDead) {
            using(var prog = new PassProgress("Eliminating Dead Code")) {
                DeadCodePass.Progress = prog;
                await DeadCodePass.Execute();
            }
        }

        if(Common.Args.Rename) {
            using(var prog = new PassProgress("Renaming Symbols")) {
                RenamePass.Progress = prog;
                await RenamePass.Execute();
            }
        }

        foreach(var diag in diags) {
            Console.ForegroundColor = diag.Severity switch {
                DiagnosticSeverity.Error => ConsoleColor.Red,
                DiagnosticSeverity.Warning => ConsoleColor.Yellow,
                DiagnosticSeverity.Info => ConsoleColor.White,
                DiagnosticSeverity.Hidden => ConsoleColor.Gray,
                var _ => ConsoleColor.White,
            };
            Console.WriteLine(diag);
        }

        Console.ResetColor();

        return await Preprocessor.Build(Common);
    }

    private void GetDocuments(ProjectId id, HashSet<ProjectId> loadedProjects) {
        if(loadedProjects.Contains(id)) { return; }
        loadedProjects.Add(id);

        foreach(var doc in Common.Solution.GetProject(id)!.Documents) {
            Common.Documents.Add(doc.Id);
            if(doc.FilePath is not null && doc.Folders.FirstOrDefault() != "obj") {
                try {
                    FileInfo fi = new FileInfo(doc.FilePath);
                    InitialChars += (ulong)fi.Length;
                } catch(Exception) {

                }
            }
        }

        foreach(var dep in Common.Project.ProjectReferences) {
            GetDocuments(dep.ProjectId, loadedProjects);
        }
    }
    
    #pragma warning disable 8618 
    private ScriptBuilder() {
        workspace = MSBuildWorkspace.Create();
    }
    
    /// Find a path to a file of the given extension, using the given path hint
    private string? FindPath(string path, string? mExtension = null) {
        string extension = mExtension ?? String.Empty;
        string dirPath = path;
        bool dir = true;

        try {
            var fa = File.GetAttributes(path);
            dir = fa.HasFlag(FileAttributes.Directory);
        } catch(FileNotFoundException) {
            dirPath = "./";
            dir = true;
        }

        if(dir) {
            var withExtension =
                from file in Directory.GetFiles(dirPath)
                where Path.GetExtension(file).ToUpper().Equals(extension)
                select file;

            if(withExtension.Count() == 1) {
                return withExtension.First();
            } else {
                foreach(var file in withExtension) {
                    if(Path.GetFileNameWithoutExtension(file)
                            .Equals(Path.GetFileNameWithoutExtension(path))
                    ) {
                        return file;
                    }
                }
            }
        } else {
            return path;
        }

        return null;
    }

    async private Task<Project> Init(string slnPath, string projectPath) {
        string slnFile = slnPath, projectFile = projectPath;
        try {
            slnFile = FindPath(slnPath, ".SLN") ??
                throw new Exception($"Failed to find solution file using path {slnPath}");
            projectFile = FindPath(projectPath, ".CSPROJ") ??
                throw new Exception($"Failed to find project file with path {projectPath}");
        } catch(Exception e) {
            Console.WriteLine(e.Message);
        }

        using(var progress = new PassProgress($"Read solution {slnFile}")) {
            var project = await workspace.OpenProjectAsync(
                projectFile,
                new Progress<ProjectLoadProgress>(
                    loadProgress => {
                        progress.Message = $"{loadProgress.Operation} {loadProgress.FilePath}";
                        progress.Report(1);
                    }
                )
            );

            /*var sln = await workspace.OpenSolutionAsync(
                slnPath,
                new Progress<ProjectLoadProgress>(
                    loadProgress => {
                        progress.Message = loadProgress.Operation.ToString();
                        progress.Report(1);
                    }
                )
            );*/

            /*var envProject = 
                sln
                .Projects
                .SingleOrDefault(p => p.Name == "env") ?? throw new Exception("No env.csproj added to solution file");*/

            // Now we use the MSBuild apis to load and evaluate our project file
            using var xmlReader = XmlReader.Create(
                File.OpenRead(Path.Join(Path.GetDirectoryName(slnPath), "env.csproj"))
            );
            ProjectRootElement root = ProjectRootElement.Create(
                xmlReader,
                new MSBuildProjectCollection(),
                preserveFormatting: true
            );
            MSBuildProject msbuildProject = new MSBuildProject(root);

            scriptDir = msbuildProject.GetPropertyValue("SpaceEngineersScript");
            if(scriptDir.Length == 0) { throw new Exception("No SpaceEngineersScript property defined in env.csproj"); }

            return project;
        }
    }
}
