using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Build.Construction;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;

namespace SeBuild;

using MSBuildProject = Microsoft.Build.Evaluation.Project;
using MSBuildProjectCollection = Microsoft.Build.Evaluation.ProjectCollection;

public class ScriptWorkspaceContext: IDisposable {
    readonly MSBuildWorkspace workspace;
    string scriptDir;
    public string GameScriptDir {
        get => scriptDir;
    }

    static ScriptWorkspaceContext() { MSBuildLocator.RegisterDefaults(); }

    Solution _sln;
    
    /// <summary>Create a new <c>ScriptWorkspaceContext</c></summary>
    /// <param name="path">
    /// If <c>path</c> is a file, then attempt to open a solution from the given file
    /// Otherwise, search through the directory that <c>path</c> points to to find a .sln file
    /// </param>
    static async public Task<ScriptWorkspaceContext> Create(string path) {
        var me = new ScriptWorkspaceContext();
        await me.Init(path);
        return me;
    }

    public void Dispose() {
        workspace.Dispose();
    }
    
    async public Task<List<CSharpSyntaxNode>> BuildProject(string name, bool rename = false, bool eliminateDead = false) {
        var project = _sln
            .Projects
            .SingleOrDefault(p => p.Name == name) ?? throw new Exception($"Solution does not contain a project with name ${name}");
        return await BuildProject(project.Id, rename, eliminateDead);
    }

    /// <summary>Build the given <c>Project</c> and return a list of declaration <c>CSharpSyntaxNode</c>s</summary>
    async public Task<List<CSharpSyntaxNode>> BuildProject(ProjectId p, bool rename = false, bool eliminateDead = false) {
        var docs = new List<Document>();
        Solution final = _sln;
        
        if(eliminateDead) {
            GetDocuments(final, p, docs);
            final = await DeadCodeRemover.Build(
                final,
                final.GetProject(p) ?? throw new Exception($"Failed to get project with ID {p}"),
                docs
            );
        }

        if(rename) {
            final = await RenameAllSymbols(final, p, new HashSet<ProjectId>());
        }

        GetDocuments(final, p, docs);

        foreach(var diag in workspace.Diagnostics) {
            Console.WriteLine(diag);
        }

        return await Preprocessor.Build(docs);
    }

    
    /// <summary>
    /// Minify a project and all dependencies of the project
    /// </summary>
    async private Task<Solution> RenameAllSymbols(Solution sol, ProjectId p, HashSet<ProjectId> renamedProjects, Renamer? other = null) {
        if(renamedProjects.Contains(p)) return sol; 
        var renamer = other is null ? new Renamer(workspace, sol, p) : new Renamer(workspace, sol, p, other);
        sol = await renamer.Run();
        var newproj = sol.GetProject(p) ?? throw new Exception($"Internal: renamer removed project {p}");
        
        renamedProjects.Add(p);

        foreach(var reference in newproj.ProjectReferences) {
            sol = await RenameAllSymbols(sol, reference.ProjectId, renamedProjects, renamer);
        }

        return sol;
    }

    private void GetDocuments(Solution sln, ProjectId id, List<Document> docs) {
        docs.Clear();
        GetDocuments(sln, id, docs, new HashSet<ProjectId>());
    }

    private void GetDocuments(Solution sln, ProjectId id, List<Document> docs, HashSet<ProjectId> loadedProjects) {
        if(loadedProjects.Contains(id)) { return; }
        loadedProjects.Add(id);

        var proj = sln.GetProject(id)
            ?? throw new Exception($"Failed to find project in solution {sln.FilePath} with ID {id}");
        foreach(var doc in proj.Documents) {
            docs.Add(doc);
        }

        foreach(var dep in proj.ProjectReferences) {
            GetDocuments(sln, dep.ProjectId, docs, loadedProjects);
        }
    }
    
    #pragma warning disable 8618 
    private ScriptWorkspaceContext() {
        workspace = MSBuildWorkspace.Create();
    }

    async private Task Init(string path) {
        string slnPath = path;
        bool dir = true;
        try {
            var fa = File.GetAttributes(slnPath);
            dir = fa.HasFlag(FileAttributes.Directory);
        } catch(FileNotFoundException) {}

        if(dir) {
            foreach(var file in Directory.GetFiles(path)) {
                if(Path.GetExtension(file).ToUpper().Equals(".SLN")) {
                    slnPath = file;
                    break;
                }
            }
        }

        Console.WriteLine($"Reading solution file {slnPath}");
        
        _sln = await workspace.OpenSolutionAsync(slnPath);
        var envProject = _sln
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
    }
}
