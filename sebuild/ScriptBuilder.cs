using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Build.Construction;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;
using Sebuild;

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

    Solution sln;
    private HashSet<ProjectId> loadedDocs = new HashSet<ProjectId>();
    
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
    
    async public Task<List<CSharpSyntaxNode>> BuildProject(string name, bool rename = false) {
        var project = sln.Projects.SingleOrDefault(p => p.Name == name) ?? throw new Exception($"Solution does not contain a project with name ${name}");
        return await BuildProject(project.Id, rename);
    }

    
    /// <summary>Build the given <c>Project</c> and return a list of declaration <c>CSharpSyntaxNode</c>s</summary>
    async public Task<List<CSharpSyntaxNode>> BuildProject(ProjectId p, bool rename = false) {
        var docs = new List<Document>();
        if(rename) {
            var mini = await MinifyProject(sln, p, docs);
            loadedDocs.Clear();
            GetDocuments(mini, p, docs);
        } else {
            GetDocuments(sln, p, docs);
        }
        foreach(var diag in workspace.Diagnostics) {
            Console.WriteLine(diag);
        }

        return await Preprocessor.Build(docs);
    }
    
    /// <summary>
    /// Minify a project and all dependencies of the project
    /// </summary>
    async private Task<Solution> MinifyProject(Solution sol, ProjectId p, List<Document> docs, PreMinifier? other = null) {
        if(loadedDocs.Contains(p)) return sol; 
        var mini = other is null ? new PreMinifier(workspace, sol, p) : new PreMinifier(workspace, sol, p, other);
        var (minisol, newproj) = await mini.Run();
        sol = minisol;
        
        loadedDocs.Add(p);

        foreach(var reference in newproj.ProjectReferences) {
            sol = await MinifyProject(sol, reference.ProjectId, docs, mini);
        }

        return sol;
    }

    private void GetDocuments(Solution sln, ProjectId id, List<Document> docs) {
        var getProj = (ProjectId id) => sln.GetProject(id) ?? throw new Exception($"Failed to find project in solution {sln.FilePath} with ID {id}");
        if(loadedDocs.Contains(id)) { return; }
        loadedDocs.Add(id);

        var proj = getProj(id);
        foreach(var doc in proj.Documents) {
            docs.Add(doc);
        }

        foreach(var dep in proj.ProjectReferences) {
            GetDocuments(sln, dep.ProjectId, docs);
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
        
        sln = await workspace.OpenSolutionAsync(slnPath);
        var envProject = sln.Projects.SingleOrDefault(p => p.Name == "env") ?? throw new Exception("No env.csproj added to solution file"); 

        // Now we use the MSBuild apis to load and evaluate our project file
        using var xmlReader = XmlReader.Create(File.OpenRead(envProject.FilePath ?? throw new Exception("Failed to locate env.csproj file")));
        ProjectRootElement root = ProjectRootElement.Create(xmlReader, new MSBuildProjectCollection(), preserveFormatting: true);
        MSBuildProject msbuildProject = new MSBuildProject(root);

        scriptDir = msbuildProject.GetPropertyValue("SpaceEngineersScript");
        if(scriptDir.Length == 0) { throw new Exception("No SpaceEngineersScript property defined in env.csproj"); }
    }

    }
