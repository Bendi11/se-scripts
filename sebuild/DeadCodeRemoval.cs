using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

    class DeadCodeRewriter: CSharpSyntaxRewriter {
        
    }

    private async Task<bool> IsSymbolReferenced(ISymbol sym) {
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
    
                var comp = await _proj.GetCompilationAsync();
                var sema = comp?.GetSemanticModel(sourceNode);

                var symbol = sema?.GetDeclaredSymbol(node);
                if(symbol is not null && (alive = await IsSymbolReferenced(symbol))) {
                    break;
                }
            } 
        }

        _alive.Add(sym, alive);
        return alive;
    }
}
