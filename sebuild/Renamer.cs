using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SeBuild;

public class Renamer {
        readonly Solution _sln;
        ProjectId _project;
        readonly NameGenerator _gen = new NameGenerator();
        Solution _final;
        public HashSet<string> Renamed = new HashSet<string>();
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

        public Renamer(Workspace ws, Solution sln, ProjectId project, Renamer other) : this(ws, sln, project) {
            Renamed = other.Renamed;
            _gen = other._gen;
        }

        public Renamer(Workspace ws, Solution sln, ProjectId project) {
            _sln = sln;
            _final = _sln;
            _project = project;
        }

        async public Task<(Solution, Project)> Run() {
            Project project;
            for(;;) {
                project = _final.GetProject(_project)
                    ?? throw new Exception($"Failed to locate project with ID {_project}");
                var comp = await project.GetCompilationAsync()
                    ?? throw new Exception($"Failed to get compilation for {project.Name}");
                bool renamedInDoc = false;
                foreach(var doc in project.Documents) {
                    var tree = await doc.GetSyntaxTreeAsync()
                        ?? throw new Exception($"Failed to get syntax tree for document {doc.FilePath}");
                    var sema = comp.GetSemanticModel(tree)
                        ?? throw new Exception($"Failed to get semantic model for document {doc.FilePath}");
                    
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

            return (_final, project);
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
                        (!symbol.CanBeReferencedByName) ||
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
            Console.WriteLine($"Renaming {symbol.ToDisplayString()} to {name}");
        _final = await Microsoft.CodeAnalysis.Rename.Renamer.RenameSymbolAsync(_final, symbol, _opts, name);
            Renamed.Add(name);
        }
    }

