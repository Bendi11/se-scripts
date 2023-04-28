using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace SeBuild;

public class DeadCodeRemover {
    Dictionary<ISymbol, bool> _alive = new Dictionary<ISymbol, bool>();
    Solution _sln;

    Project _proj;

    public DeadCodeRemover(Solution sln, Project proj) {
        _sln = sln;
        _proj = proj;
    }

    public static async Task<(Solution, Project)> Build(Solution sln, Project proj) =>
        await new DeadCodeRemover(sln, proj).Build();

    public async Task<(Solution, Project)> Build() {
        foreach(var doc in _proj.Documents) {
            var syntax = await doc.GetSyntaxRootAsync() as CSharpSyntaxNode;
            if(syntax is null) { continue; }
            
            var removed = Run(syntax);
            if(removed is null) { continue; }
            _sln = _sln.WithDocumentSyntaxRoot(doc.Id, removed);
        }

        return (_sln, _sln.GetProject(_proj.Id) ?? throw new Exception("Should never be null"));
    }

    public SyntaxNode? Run(CSharpSyntaxNode node) =>
        node.Accept(new DeadCodeRewriter(this));

    class DeadCodeRewriter: CSharpSyntaxRewriter {
        DeadCodeRemover _parent;

        public DeadCodeRewriter(DeadCodeRemover parent) {
            _parent = parent;
        }

        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node) =>
            Referenced(node) ? node : null;

        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node) =>
            Referenced(node) ? node : null;

        bool Referenced(CSharpSyntaxNode node) =>
            Task.Run(async () => await _parent.IsSyntaxReferenced(node)).Result;
    }

    async Task<ISymbol?> GetSymbol(SyntaxNode node) {
        var comp = await _proj.GetCompilationAsync();
        var sema = comp?.GetSemanticModel(node.SyntaxTree);

        return sema?.GetDeclaredSymbol(node);
    }

    public async Task<bool> IsSyntaxReferenced(SyntaxNode node) {
        var symbol = await GetSymbol(node);
        return symbol is null ? true : await IsSymbolReferenced(symbol);
    }

    public async Task<bool> IsSymbolReferenced(ISymbol sym) {
        if(_alive.ContainsKey(sym)) { return _alive[sym]; }
        
        bool alive = false;
        var refs = await SymbolFinder.FindReferencesAsync(sym, _sln);
        foreach(var reference in refs) {
            foreach(var loc in reference.Locations) {
                var sourceNode = loc.Location.SourceTree;
                if(sourceNode is null) { continue; }

                var sourceTree = await sourceNode.GetRootAsync();

                var node = sourceTree?.FindNode(loc.Location.SourceSpan);
                if(node is null) { continue; }
    
                var symbol = await GetSymbol(node); 
                if(symbol is not null && (alive = await IsSymbolReferenced(symbol))) {
                    break;
                }
            } 
        }

        _alive.Add(sym, alive);
        return alive;
    }
}
