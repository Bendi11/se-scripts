using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.Build.Construction;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Build.Locator;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SeBuild;

using MSBuildProject = Microsoft.Build.Evaluation.Project;
using MSBuildProjectCollection = Microsoft.Build.Evaluation.ProjectCollection;

public class ScriptWorkspaceContext: IDisposable {
    readonly MSBuildWorkspace workspace;
    string scriptDir;
    public string GameScriptDir {
        get => scriptDir;
    }

    Solution sln;

    static ScriptWorkspaceContext() { MSBuildLocator.RegisterDefaults(); }
    
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
    
    async public Task<NamespaceDeclarationSyntax> BuildProject(string name) {
        var project = sln.Projects.Single(p => p.Name == name) ?? throw new Exception($"Solution does not contain a project with name ${name}");
        return await BuildProject(project);
    }

    async public Task<NamespaceDeclarationSyntax> BuildProject(ProjectId id) => await ProjectBuilder.Build(
            sln,
            sln.GetProject(id) ?? throw new Exception($"Solution does not contain a project with id ${id}")
    );
    async public Task<NamespaceDeclarationSyntax> BuildProject(Project p) {
        return await ProjectBuilder.Build(sln, p);
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
        var envProject = sln.Projects.SingleOrDefault(p => p.Name == "env") ?? throw new Exception("No env.csproj defined"); 

        // Now we use the MSBuild apis to load and evaluate our project file
        using var xmlReader = XmlReader.Create(File.OpenRead(envProject.FilePath ?? throw new Exception("Failed to locate env.csproj file")));
        ProjectRootElement root = ProjectRootElement.Create(xmlReader, new MSBuildProjectCollection(), preserveFormatting: true);
        MSBuildProject msbuildProject = new MSBuildProject(root);

        scriptDir = msbuildProject.GetPropertyValue("SpaceEngineersScript");
        if(scriptDir.Length == 0) { throw new Exception("No SpaceEngineersScript property defined in env.csproj"); }
    }

    /// <summary>
    /// Builds a single .csproj project: resolves project references, removes header / footer code, minifies, and returns the
    /// final script output
    /// </summary>
    private class ProjectBuilder {
        private class NamespaceContainer {
            public NamespaceDeclarationSyntax ns;
            public NamespaceContainer(NamespaceDeclarationSyntax name) { ns = name; }
        }
        public readonly Project Project;
        private readonly Solution sln;
        private HashSet<ProjectId> loaded;
        NamespaceContainer ns;
    
        async public static Task<NamespaceDeclarationSyntax> Build(Solution sol, Project proj) => await new ProjectBuilder(sol, proj).Finish();
    
        private ProjectBuilder(Solution sol, Project proj, HashSet<ProjectId>? load = null, NamespaceContainer? name = null) {
            this.Project = proj;
            this.sln = sol;
            loaded = load ?? new HashSet<ProjectId>();
            loaded.Add(proj.Id);
            ns = name ?? new NamespaceContainer(NamespaceDeclaration(IdentifierName("")));
        }
        
        private class PreprocessWalker: CSharpSyntaxWalker {
            NamespaceContainer ns;
    
            public PreprocessWalker(NamespaceContainer name) : base(SyntaxWalkerDepth.Trivia) {
                ns = name;
            }
            
            public override void VisitClassDeclaration(ClassDeclarationSyntax node) {
                Console.WriteLine(node.GetText());
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
                    ns.ns = ns.ns.AddMembers(node.Members.ToArray());
                } else {
                    ns.ns = ns.ns.AddMembers(node);
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
                await new ProjectBuilder(sln, project, loaded, ns).Finish();
            }
        }
    
        async public Task<NamespaceDeclarationSyntax> Finish() {
            await ResolveRefs();
            foreach(var doc in from doc in Project.Documents where doc.Folders.Count == 0 || doc.Folders.First() != "obj" select doc) {
                Console.WriteLine($"Processing {doc.Name}");
                await Digest(doc);
            }
            
            return this.ns.ns;
        }
    
        async private Task Digest(Document doc) {
            OptionSet opts = await doc.GetOptionsAsync();
            opts = FormatterOpts.Apply(opts);
            var newdoc = await Formatter.FormatAsync(doc, opts);
            var syntax = await newdoc.GetSyntaxTreeAsync() as CSharpSyntaxTree ?? throw new Exception("Cannot compile non-C# files");
            syntax
                .GetRoot()
                .Accept(new PreprocessWalker(this.ns));
        }
    }
}
