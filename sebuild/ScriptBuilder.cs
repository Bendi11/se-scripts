using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

    Solution sln;
    
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
    
    async public Task<List<CSharpSyntaxNode>> BuildProject(string name) {
        var project = sln.Projects.SingleOrDefault(p => p.Name == name) ?? throw new Exception($"Solution does not contain a project with name ${name}");
        return await BuildProject(project);
    }

    async public Task<List<CSharpSyntaxNode>> BuildProject(ProjectId id) => await Preprocessor.Build(
            sln,
            sln.GetProject(id) ?? throw new Exception($"Solution does not contain a project with id ${id}")
    );
    
    /// <summary>Build the given <c>Project</c> and return a list of declaration <c>CSharpSyntaxNode</c>s</summary>
    async public Task<List<CSharpSyntaxNode>> BuildProject(Project p) {
        foreach(var diag in workspace.Diagnostics) {
            Console.WriteLine(diag);
        }
        return await Preprocessor.Build(sln, p);
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

    /// <summary>
    /// Builds a single .csproj project: resolves project references, removes header / footer code, and returns the
    /// final declarations list
    /// </summary>
    private class Preprocessor {
        public readonly Project Project;
        private readonly Solution sln;
        private HashSet<ProjectId> loaded;
        List<CSharpSyntaxNode> decls;
    
        async public static Task<List<CSharpSyntaxNode>> Build(Solution sol, Project proj) => await new Preprocessor(sol, proj).Finish();
    
        private Preprocessor(Solution sol, Project proj, HashSet<ProjectId>? load = null, List<CSharpSyntaxNode>? dec = null) {
            this.Project = proj;
            this.sln = sol;
            loaded = load ?? new HashSet<ProjectId>();
            loaded.Add(proj.Id);
            decls = dec ?? new List<CSharpSyntaxNode>();
        }
        
        private class PreprocessWalker: CSharpSyntaxWalker {
            List<CSharpSyntaxNode> decls;
            public PreprocessWalker(List<CSharpSyntaxNode> dec) : base(SyntaxWalkerDepth.Trivia) {
                decls = dec;
            }
            
            public override void VisitClassDeclaration(ClassDeclarationSyntax node) {
                if(
                        node.Identifier.ValueText.Equals("Program") &&
                        (
                            node
                                .BaseList
                                ?.Types
                                .AsEnumerable()
                                .Any(ty => (ty.Type as IdentifierNameSyntax)?.Identifier.ValueText.Equals("MyGridProgram") ?? false)
                            ?? false
                        )
                ) {
                    decls.AddRange(node.Members);
                } else {
                    decls.Add(node);
                }
            }
        }

        /// <summary>
        /// Resolve all inter-project references by emitting their code to this <c>ProjectBuilder</c>'s <c>ns</c>
        /// </summary>
        async private Task ResolveRefs() {
            foreach(var reference in Project.ProjectReferences) {
                if(loaded.Contains(reference.ProjectId)) { continue; }
                var project = sln.GetProject(reference.ProjectId) ?? throw new Exception($"Cannot locate referenced project {reference.ProjectId}");
                Console.WriteLine($"Resolving project reference {project.Name}");
                await new Preprocessor(sln, project, loaded, decls).Finish();
            }
        }
    
        async private Task<List<CSharpSyntaxNode>> Finish() {
            await ResolveRefs();
            Console.WriteLine($"{Project.Name}");
            foreach(var doc in from doc in Project.Documents where doc.Folders.FirstOrDefault() != "obj" select doc) {
                Console.WriteLine($"Processing {doc.FilePath}");
                await Digest(doc);
            }
            
            return this.decls;
        }
    
        async private Task Digest(Document doc) {
            //var dOpts = await doc.GetOptionsAsync();
            //var opts = FormatterOpts.Apply(dOpts);
            //var newdoc = await Formatter.FormatAsync(doc, opts);
            var syntax = await doc.GetSyntaxTreeAsync() as CSharpSyntaxTree ?? throw new Exception("Cannot compile non-C# files");
            syntax
                .GetRoot()
                .Accept(new PreprocessWalker(decls));
        }
    }
}
