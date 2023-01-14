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
    private HashSet<ProjectId> minified = new HashSet<ProjectId>();
    
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
        Solution newsln;
        Project proj;
        if(rename) {
            (newsln, proj) = await MinifyProject(sln, p);
        } else {
            newsln = sln;
            proj = sln.GetProject(p) ?? throw new Exception($"Failed to find project in solution {sln.FilePath} with ID {p}");
        }
        foreach(var diag in workspace.Diagnostics) {
            Console.WriteLine(diag);
        }

        return await Preprocessor.Build(newsln, proj);
    }
    
    /// <summary>
    /// Minify a project and all dependencies of the project
    /// </summary>
    async private Task<(Solution, Project)> MinifyProject(Solution sol, ProjectId p, PreMinifier? other = null) {
        if(minified.Contains(p)) { return (sln, sln.GetProject(p) ?? throw new Exception($"Solution has no project with ID {p}")); } 
        var mini = other is null ? new PreMinifier(workspace, sol, p) : new PreMinifier(workspace, sol, p, other);
        sol = await mini.Run();
        minified.Add(p);
        var fetchProject = () => sol.GetProject(p) ?? throw new Exception($"Failed to locate project with ID {p} after minification");

        foreach(var reference in fetchProject().ProjectReferences) {
            var (newsol, _) = await MinifyProject(sol, reference.ProjectId, mini);
            sol = newsol;
        }



        return (sol, fetchProject());
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
        ProjectId _project;
        Compilation _comp;
        readonly NameGenerator _gen = new NameGenerator();
        Solution _final;
        public HashSet<string> Renamed = new HashSet<string>();
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
                StringBuilder sb = new StringBuilder();
                
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

        public PreMinifier(Workspace ws, Solution sln, ProjectId project, PreMinifier other) : this(ws, sln, project) {
            Renamed = other.Renamed;
            _gen = other._gen;
        }

        public PreMinifier(Workspace ws, Solution sln, ProjectId project) {
            _sln = sln;
            _final = _sln;
            _project = project;
        }

        async public Task<Solution> Run() {
            for(;;) {
                var project = _final.GetProject(_project) ?? throw new Exception($"Failed to locate project with ID {_project}");
                _comp = await project.GetCompilationAsync() ?? throw new Exception($"Failed to get compilation for {project.Name}");
                bool renamedInDoc = false;
                foreach(var doc in project.Documents) {
                    var tree = await doc.GetSyntaxTreeAsync() ?? throw new Exception($"Failed to get syntax tree for document {doc.FilePath}");
                    var sema = _comp.GetSemanticModel(tree) ?? throw new Exception($"Failed to get semantic model for document {doc.FilePath}");
                    var walker = new RenamerWalker(Renamed, sema);
                    walker.Visit(await tree.GetRootAsync());
                    if(walker.ToRename is not null) {
                        await RandomName(sema, walker.ToRename);
                        renamedInDoc = true;
                        break;
                    }
                }

                if(!renamedInDoc) { break; }
            }

            return _final;
        }

        private class RenamerWalker: CSharpSyntaxWalker {
            private HashSet<string> _renamed;
            private SemanticModel _sema;
            public ISymbol? ToRename { get; private set; } = null;

            public RenamerWalker(HashSet<string> renamed, SemanticModel sema) {
                _renamed = renamed;
                _sema = sema;
            }

            public override void Visit(SyntaxNode? node) {
                if(ToRename is null) { base.Visit(node); }
            }

            public override void VisitClassDeclaration(ClassDeclarationSyntax node) {
                AttemptRename(node);
                base.VisitClassDeclaration(node);
            }

            public override void VisitEnumDeclaration(EnumDeclarationSyntax node) {
                AttemptRename(node);
                base.VisitEnumDeclaration(node);
            }

            public override void VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node) {
                AttemptRename(node);
                base.VisitEnumMemberDeclaration(node);
            }

            public override void VisitMethodDeclaration(MethodDeclarationSyntax node) {
                AttemptRename(node);
                base.VisitMethodDeclaration(node);
            }

            public override void VisitParameter(ParameterSyntax param) {
                AttemptRename(param);
                base.VisitParameter(param);
            }

            public override void VisitTypeParameter(TypeParameterSyntax tparam) {
                AttemptRename(tparam);
                base.VisitTypeParameter(tparam);
            }

            public override void VisitVariableDeclaration(VariableDeclarationSyntax vbl) {
                if(ToRename is not null) { return; }
                foreach(var name in vbl.Variables) {
                    AttemptRename(name); 
                }
            }

            public override void VisitForEachStatement(ForEachStatementSyntax frch) {
                AttemptRename(frch);
                base.VisitForEachStatement(frch);
            }

            private void AttemptRename(SyntaxNode node) {
                if(ToRename is not null) { return; }
                var symbol = _sema.GetDeclaredSymbol(node) ?? throw new Exception($"Failed to get symbol for syntax {node.GetText()}");
                if(
                        (symbol is INamedTypeSymbol && (symbol.Name.Equals("Program"))) ||
                        (symbol is IMethodSymbol) && (symbol.Name.Equals("Save") || symbol.Name.Equals("Main"))
                ) {
                    return;
                }

                if(!_renamed.Contains(symbol.Name)) {
                    ToRename = symbol;
                    return;
                }
            }
        }

        async private Task RandomName(SemanticModel sema, ISymbol symbol) {
            string name = _gen.Next();
            bool conflicts = true;
            while(conflicts) {
                conflicts = false;
                foreach(var loc in symbol.Locations) {
                    if(sema.LookupSymbols(loc.SourceSpan.Start).Any(collider => collider.Name.Equals(name))) {
                        conflicts = true;
                        break;
                    }
                }

                conflicts = conflicts || Renamed.Contains(name);

                if(conflicts) {
                    Console.WriteLine($"Generated symbol {name} (replaces {symbol.Name}) collides, regenerating...");
                    name = _gen.Next();
                }
            }
            Console.WriteLine($"Renaming {symbol.Name} to {name}");
            _final = await Renamer.RenameSymbolAsync(_final, symbol, _opts, name);
            Renamed.Add(name);
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
