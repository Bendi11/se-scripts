using System.Xml;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Rename;
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
        var mini = await PreMinifier.Create(sln, p);
        var newsln = await mini.Run();
        workspace.TryApplyChanges(newsln);
        foreach(var diag in workspace.Diagnostics) {
            Console.WriteLine(diag);
        }
        return await Preprocessor.Build(newsln, p);
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

    private class PreMinifier {
        readonly Solution _sln;
        readonly Project _project;
        Compilation _comp;
        readonly NameGenerator _gen = new NameGenerator();
        Solution _final;
        static readonly SymbolRenameOptions _opts = new SymbolRenameOptions() {
            RenameOverloads = true,
            RenameFile = false,
            RenameInComments = false,
            RenameInStrings = false,
        };
        
        private class NameGenerator {
            int _nChars = 1;
            List<IEnumerator<char>> _gen = new List<IEnumerator<char>>();

            public NameGenerator() {
                _gen.Add(UnicodeEnumerator());
            }

            private void IncrementSlot(int slot) {
                if(slot >= _gen.Count) {
                    _gen.Append(UnicodeEnumerator());
                    return;
                }

                if(!_gen[slot].MoveNext()) {
                    _gen[slot] = UnicodeEnumerator();
                    IncrementSlot(slot + 1);
                }
            }

            public string Next() {
                StringBuilder sb = new StringBuilder('_');
                
                IncrementSlot(0);
                foreach(var slot in _gen) {
                    sb.Append(slot.Current);
                }
                
                return sb.ToString();
            }

            IEnumerator<char> UnicodeEnumerator() {
                IEnumerable<char> Range(int from, int to) {
                    for(int i = from; i <= to; ++i) { yield return (char)i; }
                }
                
                var letters = Range(0x41, 0x5A)
                    .Concat(Range(0x61, 0x7A))
                    .Concat(Range(0xC0, 0xFF))
                    .Concat(Range(0x100, 0x17F))
                    .Concat(Range(0x180, 0x1BF))
                    .Concat(Range(0x1C4, 0x1CC))
                    .Concat(Range(0x1CD, 0x1DC))
                    .Concat(Range(0x1DD, 0x1FF))
                    .Concat(Range(0x200, 0x217))
                    .Concat(Range(0x218, 0x21B))
                    .Concat(Range(0x21C, 0x24F))
                    .Concat(Range(0x22A, 0x233))
                    .Concat(Range(0x234, 0x236))
                    .Concat(Range(0x238, 0x240))
                    .Concat(Range(0x23A, 0x23E))
                    .Concat(Range(0x250, 0x2A8))
                    .Concat(Range(0x2A9, 0x2AD))
                    .Concat(Range(0x2AE, 0x2AF))
                    .Concat(Range(0x370, 0x3FB))
                    .Concat(Range(0x37B, 0x37D))
                    .Concat(Range(0x37F, 0x3F3))
                    .Concat(Range(0x3CF, 0x3F9))
                    .Concat(Range(0x3E2, 0x3EF))
                    .Concat(Range(0x400, 0x45F))
                    .Concat(Range(0x410, 0x44F))
                    .Concat(Range(0x460, 0x481))
                    .Concat(Range(0x48A, 0x4F9))
                    .Concat(Range(0x4FA, 0x4FF))
                    .Concat(Range(0x500, 0x52D))
                    .Concat(Range(0x531, 0x556))
                    .Concat(Range(0x560, 0x588))
                    .Concat(Range(0x10A0, 0x10C5))
                    .Concat(Range(0x10D0, 0x10F0))
                    .Concat(Range(0x13A0, 0x13F4))
                    .Concat(Range(0x1C90, 0x1CB0))
                    .Concat(Range(0x1E00, 0x1EF9))
                    .Concat(Range(0x1EA0, 0x1EF1))
                    .Concat(Range(0x1F00, 0x1FFC))
                    .Concat(Range(0x2C00, 0x2C2E))
                    .Concat(Range(0x2C30, 0x2C5E));

                foreach(var letter in letters) { yield return letter; }
            }
        }

        static async public Task<PreMinifier> Create(Solution sln, Project project) {
            var me = new PreMinifier(sln, project);
            await me.Init();
            
            return me;
        }

        async private Task Init() {
            _comp = await _project.GetCompilationAsync() ?? throw new Exception($"Failed to get compilation for project {_project.Name}");
        }

        private PreMinifier(Solution sln, Project project) {
            _sln = sln;
            _project = project;
        }

        private IEnumerable<INamedTypeSymbol> GetLocalTypes(INamespaceSymbol ns) => ns.GetTypeMembers().Where(ty => ty.Locations.Any(loc => loc.IsInSource));

        async public Task<Solution> Run() {
            foreach(var ns in _comp.GlobalNamespace.GetNamespaceMembers()) {
                await RenameGroup(GetLocalTypes(ns));
            }

            return _final;
        }

        async public Task RenameGroup(IEnumerable<ISymbol> types) {
            foreach(var dec in types) {
                string name = _gen.Next();
                Console.WriteLine($"Renaming {dec.Name} to {name}");
                _final = await Renamer.RenameSymbolAsync(_sln, dec, _opts, name);
                if(dec is INamedTypeSymbol ty) {
                    await RenameGroup(ty.GetMembers().Where(member => member is IFieldSymbol));
                }
            }
        }
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
