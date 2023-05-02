using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Build.Construction;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;

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
        var sln = await me.Init(args.SolutionPath);

        var projectId = (
            sln
                .Projects
                .SingleOrDefault(p => p.Name == args.Project)?? throw new Exception($"Solution does not contain a project with name ${args.Project}")
        ).Id;

        
        me.Common = new ScriptCommon(sln, projectId, new List<DocumentId>(), args);
        
        me.GetDocuments(projectId, new HashSet<ProjectId>());
        me.DeadCodePass = new DeadCodeRemover(me.Common);
        me.RenamePass = new Renamer(me.Common);
        return me;
    }

    public void Dispose() {
        workspace.Dispose();
    }

    /// <summary>Build the given <c>Project</c> and return a list of declaration <c>CSharpSyntaxNode</c>s</summary>
    async public Task<List<CSharpSyntaxNode>> BuildProject() {
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

        foreach(var diag in workspace.Diagnostics) {
            Console.WriteLine(diag);
        }

        return await Preprocessor.Build(Common);
    }

    private void GetDocuments(ProjectId id, HashSet<ProjectId> loadedProjects) {
        if(loadedProjects.Contains(id)) { return; }
        loadedProjects.Add(id);

        foreach(var doc in Common.Solution.GetProject(id)!.DocumentIds) {
            Common.Documents.Add(doc);
        }

        foreach(var dep in Common.Project.ProjectReferences) {
            GetDocuments(dep.ProjectId, loadedProjects);
        }
    }
    
    #pragma warning disable 8618 
    private ScriptBuilder() {
        workspace = MSBuildWorkspace.Create();
    }

    async private Task<Solution> Init(string slnPath) {
        bool dir = true;
        try {
            var fa = File.GetAttributes(slnPath);
            dir = fa.HasFlag(FileAttributes.Directory);
        } catch(FileNotFoundException) {}

        if(dir) {
            foreach(var file in Directory.GetFiles(slnPath)) {
                if(Path.GetExtension(file).ToUpper().Equals(".SLN")) {
                    slnPath = file;
                    break;
                }
            }
        }

        Console.WriteLine($"Reading solution file {slnPath}");
        
        var sln = await workspace.OpenSolutionAsync(slnPath);
        var envProject = 
            sln
            .Projects
            .SingleOrDefault(p => p.Name == "env") ?? throw new Exception("No env.csproj added to solution file"); 

        // Now we use the MSBuild apis to load and evaluate our project file
        using var xmlReader = XmlReader.Create(
            File.OpenRead(envProject.FilePath ?? throw new Exception("Failed to locate env.csproj file"))
        );
        ProjectRootElement root = ProjectRootElement.Create(
            xmlReader,
            new MSBuildProjectCollection(),
            preserveFormatting: true
        );
        MSBuildProject msbuildProject = new MSBuildProject(root);

        scriptDir = msbuildProject.GetPropertyValue("SpaceEngineersScript");
        if(scriptDir.Length == 0) { throw new Exception("No SpaceEngineersScript property defined in env.csproj"); }

        return sln;
    }
}
