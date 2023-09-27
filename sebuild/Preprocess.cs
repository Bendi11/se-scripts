using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SeBuild;

/// <summary>
/// Builds a single .csproj project: resolves project references, removes header / footer code, and returns the
/// final declarations list
/// </summary>
public class Preprocessor {
    ScriptCommon Common;
    ConcurrentBag<CSharpSyntaxNode> decls;

    async public static Task<IEnumerable<CSharpSyntaxNode>> Build(ScriptCommon ctx) => await new Preprocessor(ctx).Finish(); 

    private Preprocessor(ScriptCommon ctx, ConcurrentBag<CSharpSyntaxNode>? dec = null) {
        Common = ctx;
        decls = dec ?? new ConcurrentBag<CSharpSyntaxNode>();
    }
    
    private class PreprocessWalker: CSharpSyntaxWalker {
        ConcurrentBag<CSharpSyntaxNode> decls;
        public PreprocessWalker(ConcurrentBag<CSharpSyntaxNode> dec) : base(SyntaxWalkerDepth.Node) {
            decls = dec;
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node) {
            decls.Add(node);
        }

        public override void VisitEnumDeclaration(EnumDeclarationSyntax node) {
            decls.Add(node);
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node) {
            decls.Add(node);
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
                foreach(var member in node.Members) {
                    decls.Add(member);
                }
            } else {
                decls.Add(node);
            }
        }
    }

    async private Task<IEnumerable<CSharpSyntaxNode>> Finish() {
        var tasks = new List<Task>();
        foreach(var doc in from doc in Common.DocumentsIter where doc.Folders.FirstOrDefault() != "obj" select doc) {
            tasks.Add(Task.Run(async () => await Digest(doc)));
        }

        await Task.WhenAll(tasks);
        
        return this.decls;
    }

    async private Task Digest(Document doc) {
        var syntax = await doc.GetSyntaxTreeAsync() as CSharpSyntaxTree ?? throw new Exception("Cannot compile non-C# files");
        syntax
            .GetRoot()
            .Accept(new PreprocessWalker(decls));
    }
}

