using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace SeBuild;

public class DeadCodeRemover: CompilationPass {
    Dictionary<ISymbol, bool> _alive = new Dictionary<ISymbol, bool>(SymbolEqualityComparer.Default);
    DocumentId? _aliveDoc = null;

    public DeadCodeRemover(ScriptCommon ctx) : base(ctx) {
        Task.Run(async () => await Init()).Wait();
    }

    public async override Task Execute() {
        var tasks = new List<Task>();
        var map = new Dictionary<DocumentId, SyntaxNode>();

        foreach(var doc in Common.DocumentsIter) {
            tasks.Add(Task.Run(async () => {
                var syntax = await doc.GetSyntaxRootAsync() as CSharpSyntaxNode;
                if(syntax is null) { return; }
                
                var removed = syntax.Accept(new DeadCodeRewriter(this, doc.Project));
                if(removed is null) { return; }

                lock(map) {
                    map.Add(doc.Id, removed);
                }
            }));
        }

        await Task.WhenAll(tasks);

        foreach(var (newDocId, newDoc) in map) {
            Common.Solution = Common
                .Solution
                .WithDocumentSyntaxRoot(newDocId, newDoc, PreservationMode.PreserveIdentity);
        }
    }
    
    /// Add alive annotation to known alive classes
    async Task Init() {
        foreach(var doc in Common.Project.Documents) {
            var syntax = await doc.GetSyntaxRootAsync() as CSharpSyntaxNode;
            if(syntax is null) { continue; }
            var finder = new MainProgramFinder();
            syntax.Accept(finder);
            if(finder.ProgramDecl is not null) {
                var symbol = await GetSymbol(finder.ProgramDecl, doc.Project);
                if(symbol is not null) {
                    _alive.Add(symbol, true);
                    _aliveDoc = doc.Id;
                    var decl = symbol as INamedTypeSymbol;
                    if(decl is not null) {
                        foreach(var member in decl.GetMembers()) {
                            _alive.Add(member, true);
                        }
                    }
                    break;
                }
            }
        }
    }

    class MainProgramFinder: CSharpSyntaxWalker {
        public ClassDeclarationSyntax? ProgramDecl = null;

        public MainProgramFinder() {}

        public override void Visit(SyntaxNode? node) {
            if(ProgramDecl is null) { base.Visit(node); }
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
                ProgramDecl = node;
                return;
            }
            
            base.VisitClassDeclaration(node);
        }
    }

    class DeadCodeRewriter: CSharpSyntaxRewriter {
        DeadCodeRemover _parent;
        Project _project;

        public DeadCodeRewriter(DeadCodeRemover parent, Project p) {
            _parent = parent;
            _project = p;
        }

        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node) =>
            Referenced(node) ? base.VisitMethodDeclaration(node) : null;

        public override SyntaxNode? VisitFieldDeclaration(FieldDeclarationSyntax node) =>
            Referenced(node) ? base.VisitFieldDeclaration(node) : null;
        
        public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node) =>
            Referenced(node) ? base.VisitPropertyDeclaration(node) : null;

        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node) =>
            Referenced(node) ? base.VisitClassDeclaration(node) : null;

        public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node) =>
            Referenced(node) ? base.VisitStructDeclaration(node) : null;

        bool Referenced(CSharpSyntaxNode node) {
            bool referenced = Task.Run(async () => await _parent.IsSyntaxReferenced(node, _project)).Result;
            if(!referenced) {
                _parent.Tick();
            }

            return referenced;
        }
    }

    async Task<ISymbol?> GetSymbol(SyntaxNode node, Project proj) {
        var comp = await proj.GetCompilationAsync();
        var sema = comp?.GetSemanticModel(node.SyntaxTree);
        return sema?.GetDeclaredSymbol(node);
    }

    async Task<bool> IsSyntaxReferenced(SyntaxNode node, Project proj) {
        var symbol = await GetSymbol(node, proj);
        return symbol is null ? true : await IsSymbolReferenced(symbol);
    }

    async Task<bool> IsSymbolReferenced(ISymbol sym) {
        if(_alive.ContainsKey(sym)) { return _alive[sym]; }
        
        _alive.Add(sym, false);
        var refs = await SymbolFinder.FindReferencesAsync(sym, Common.Solution);
        foreach(var reference in refs) {
            foreach(var loc in reference.Locations) {
                var sourceNode = loc.Location.SourceTree;
                if(sourceNode is null) { continue; }

                var sourceTree = await sourceNode.GetRootAsync();

                var node = sourceTree?.FindNode(loc.Location.SourceSpan);

                if(_aliveDoc is not null && loc.Document.Id == _aliveDoc) {
                    return _alive[sym] = true;
                }
                if(node is null) { continue; }
                ISymbol? symbol = null;

                while(node is not null && symbol is null) {
                    symbol = await GetSymbol(node, loc.Document.Project);
                    node = node.Parent;
                }
                
                if(symbol is null) { continue; }
                if(symbol.CanBeReferencedByName) {
                    if(await IsSymbolReferenced(symbol)) {
                        return _alive[sym] = true;
                    }
                }
            } 
        }

        return false;
    }
}
