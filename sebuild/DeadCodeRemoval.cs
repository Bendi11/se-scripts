using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace SeBuild;

public class DeadCodeRemover {
    Dictionary<ISymbol, bool> _alive = new Dictionary<ISymbol, bool>(SymbolEqualityComparer.Default);
    Solution _sln;
    Document? _aliveDoc = null;
    Project _proj;

    public DeadCodeRemover(Solution sln, Project proj) {
        _sln = sln;
        _proj = proj;
    }

    public static async Task<Solution> Build(Solution sln, Project proj, List<Document> docs) =>
        await new DeadCodeRemover(sln, proj).Build(docs);

    public async Task<Solution> Build(IEnumerable<Document> docs) {
        _alive.Clear();
        await Init();
        foreach(var doc in docs) {
            _proj = doc.Project;
            var syntax = await doc.GetSyntaxRootAsync() as CSharpSyntaxNode;
            if(syntax is null) { continue; }
            
            var removed = Run(syntax);
            if(removed is null) { continue; }
            _sln = _sln.WithDocumentSyntaxRoot(doc.Id, removed, PreservationMode.PreserveIdentity);
        }

        return _sln;
    }
    
    /// Add alive annotation to known alive classes
    async Task Init() {
        foreach(var doc in _proj.Documents) {
            var syntax = await doc.GetSyntaxRootAsync() as CSharpSyntaxNode;
            if(syntax is null) { continue; }
            var finder = new MainProgramFinder();
            syntax.Accept(finder);
            if(finder.ProgramDecl is not null) {
                var symbol = await GetSymbol(finder.ProgramDecl);
                if(symbol is not null) {
                    _alive.Add(symbol, true);
                    _aliveDoc = doc;
                    var decl = symbol as INamedTypeSymbol;
                    if(decl is not null) {
                        foreach(var member in decl.GetMembers()) {
                            Console.WriteLine($"alive: {member.Name}");
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

    public SyntaxNode? Run(CSharpSyntaxNode node) =>
        node.Accept(new DeadCodeRewriter(this));

    class DeadCodeRewriter: CSharpSyntaxRewriter {
        DeadCodeRemover _parent;

        public DeadCodeRewriter(DeadCodeRemover parent) {
            _parent = parent;
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
            bool referenced = Task.Run(async () => await _parent.IsSyntaxReferenced(node)).Result;
            if(!referenced) {
                Console.WriteLine($"Eliminating dead code {node.FullSpan}");
            }

            return referenced;
        }
    }

    async Task<ISymbol?> GetSymbol(SyntaxNode node, Project? proj = null) {
        var comp = await (proj ?? _proj).GetCompilationAsync();
        var sema = comp?.GetSemanticModel(node.SyntaxTree);
        return sema?.GetDeclaredSymbol(node);
    }

    public async Task<bool> IsSyntaxReferenced(SyntaxNode node) {
        var symbol = await GetSymbol(node);
        return symbol is null ? true : await IsSymbolReferenced(symbol);
    }

    public async Task<bool> IsSymbolReferenced(ISymbol sym) {
        if(_alive.ContainsKey(sym)) { return _alive[sym]; }
        
        _alive.Add(sym, false);
        var refs = await SymbolFinder.FindReferencesAsync(sym, _sln);
        foreach(var reference in refs) {
            foreach(var loc in reference.Locations) {
                var sourceNode = loc.Location.SourceTree;
                if(sourceNode is null) { continue; }

                var sourceTree = await sourceNode.GetRootAsync();

                var node = sourceTree?.FindNode(loc.Location.SourceSpan);

                Console.WriteLine($"{sym.Name} ref @ {loc.Document.Project.Name}: {node}");
                if(loc.Document.Id == _aliveDoc.Id) {
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
