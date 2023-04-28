using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sebuild;

/// <summary>
/// Builds a single .csproj project: resolves project references, removes header / footer code, and returns the
/// final declarations list
/// </summary>
public class Preprocessor {
    public readonly List<Document> Docs;
    List<CSharpSyntaxNode> decls;

    async public static Task<List<CSharpSyntaxNode>> Build(List<Document> docs) => await new Preprocessor(docs).Finish(); 

    private Preprocessor(List<Document> docs, List<CSharpSyntaxNode>? dec = null) {
        Docs = docs;
        decls = dec ?? new List<CSharpSyntaxNode>();
    }
    
    private class PreprocessWalker: CSharpSyntaxWalker {
        List<CSharpSyntaxNode> decls;
        public PreprocessWalker(List<CSharpSyntaxNode> dec) : base(SyntaxWalkerDepth.Trivia) {
            decls = dec;
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node) => decls.Add(node);

        public override void VisitEnumDeclaration(EnumDeclarationSyntax node) => decls.Add(node);
        
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

    async private Task<List<CSharpSyntaxNode>> Finish() {
        foreach(var doc in from doc in Docs where doc.Folders.FirstOrDefault() != "obj" select doc) {
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

