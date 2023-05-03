using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace SeBuild;

public class Renamer: CompilationPass {
        readonly NameGenerator _gen = new NameGenerator();
        MultiMap<ISymbol, ReferencedSymbol> _collectedRefs =
            new MultiMap<ISymbol, ReferencedSymbol>(SymbolEqualityComparer.Default);
        MultiMap<DocumentId, TextChange> _modifications = new MultiMap<DocumentId, TextChange>();

        class MultiMap<K, V>: IEnumerable<KeyValuePair<K, List<V>>>
        where K: notnull {
            Dictionary<K, List<V>> _multiMap;


            public MultiMap(System.Collections.Generic.IEqualityComparer<K>? cmp = null) {
                _multiMap = 
                    new Dictionary<K, List<V>>(cmp);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

            public IEnumerator<KeyValuePair<K, List<V>>> GetEnumerator() =>
                _multiMap.GetEnumerator();

            public bool Contains(K symbol) =>
                _multiMap.ContainsKey(symbol);

            public void Add(K symbol, V reference) {
                List<V>? references = null;
                if(_multiMap.TryGetValue(symbol, out references)) {
                    references.Add(reference);
                } else {
                    references = new List<V>();
                    references.Add(reference);
                    _multiMap[symbol] = references;
                }
            }
            
            /// Remove all references for the given symbol, without removing it from the map
            public void Clear(K symbol) =>
                _multiMap[symbol] = new List<V>();

            public List<V>? Get(K symbol) {
                List<V>? value = null;
                _multiMap.TryGetValue(symbol, out value);
                return value;
            }
        }

        static readonly SymbolRenameOptions _opts = new SymbolRenameOptions() {
            RenameOverloads = true,
            RenameFile = false,
            RenameInComments = false,
            RenameInStrings = false,
        };
        
        /// Generator for creating random unicode names
        private struct NameGenerator {
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
                    .Concat(Range(0xC0, 0xF6))
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

        public Renamer(ScriptCommon ctx) : base(ctx) {}

        /// Get the next symbol to rename
        async Task<ISymbol?> Symbol() {
            var tasks = new List<Task>();
            foreach(var docId in Common.Documents) {
                var doc = Common.Solution.GetDocument(docId)!;
                var project = Common.Solution.GetProject(doc.Project.Id)!;
                var comp = (await project.GetCompilationAsync())!;

                var tree = (await doc.GetSyntaxTreeAsync())!;
                var sema = comp.GetSemanticModel(tree)!;
               
                var walker = new RenamerWalker(_collectedRefs, tasks, sema, Common.Solution);
                walker.Visit(await tree.GetRootAsync());
            }

            await Task.WhenAll(tasks);

            return null;
        }
        
        /// Rename all identifiers in the given project
        async public override Task Execute() {
            await Symbol();

            foreach(var (symbol, references) in _collectedRefs) {
                var newName = _gen.Next();

                Msg($"{symbol.Name} -> {newName}");
                Tick();

                foreach(var reference in references) {
                    foreach(var loc in reference.Locations) {
                        RenameReference(loc, newName);
                    }
                }
            }

            foreach(var (docId, changes) in _modifications) {
                var doc = Common.Solution.GetDocument(docId)!;
                var text = await doc.GetTextAsync();
                var newText = text.WithChanges(changes);
                Common.Solution = Common.Solution.WithDocumentText(docId, newText);
            }
        }

        private class RenamerWalker: CSharpSyntaxWalker {
            MultiMap<ISymbol, ReferencedSymbol> _references;
            List<Task> _tasks;
            SemanticModel _sema;
            Solution _sln;

            public RenamerWalker(
                MultiMap<ISymbol,ReferencedSymbol> refs,
                List<Task> tasks,
                SemanticModel sema,
                Solution sln
            ) {
                _references = refs;
                _sema = sema;
                _sln = sln;
                _tasks = tasks;
            }

            public override void VisitClassDeclaration(ClassDeclarationSyntax node) {
                AttemptRename(node);
                base.VisitClassDeclaration(node);
            }

            public override void VisitEnumDeclaration(EnumDeclarationSyntax node) {
                AttemptRename(node);
                base.VisitEnumDeclaration(node);
            }

            public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node) {
                AttemptRename(node);
                base.VisitInterfaceDeclaration(node);
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
                foreach(var name in vbl.Variables) {
                    AttemptRename(name); 
                }
            }

            public override void VisitForEachStatement(ForEachStatementSyntax frch) {
                AttemptRename(frch);
                base.VisitForEachStatement(frch);
            }

            private void AttemptRename(SyntaxNode node) {
                var symbol = _sema
                    .GetDeclaredSymbol(node)
                    ?? throw new Exception($"Failed to get symbol for syntax {node.GetText()}");

                if(symbol.Kind != SymbolKind.Namespace &&
                    !_references.Contains(symbol) &&
                    !symbol.IsImplicitlyDeclared &&
                    symbol.Locations.Any((loc) => loc.IsInSource) &&
                    !symbol.IsExtern &&
                    symbol.CanBeReferencedByName &&
                    !(symbol is INamedTypeSymbol && (symbol.Name.Equals("Program"))) &&
                    !(symbol is IMethodSymbol && (symbol.Name.Equals("Save") || symbol.Name.Equals("Main")))
                ) {
                    _references.Clear(symbol);
                    var refs = _references.Get(symbol)!;
                    _tasks.Add(Task.Run(async () => {
                        var symbolReferences = await SymbolFinder.FindReferencesAsync(symbol, _sln);
                        foreach(var reference in symbolReferences) {
                            refs.Add(reference); 
                        }
                    }));
                    return;
                }
            }
        }

        void RenameReference(ReferenceLocation refLoc, string name) {
            if(refLoc.IsImplicit) { return; }
            //var sourceNode = refLoc.Location.SourceTree;
            //if(sourceNode is null) { return; }
            
            var change = new TextChange(refLoc.Location.SourceSpan, name);
            _modifications.Add(refLoc.Document.Id, change);
        }
}
