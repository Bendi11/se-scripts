using CSharpMinifier;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

public class ScriptBuilder {
    StringBuilder sb = new StringBuilder();
    string directory;

    public ScriptBuilder(string dir) {
        MSBuildWorkspace;
        directory = dir;
    }
    
    /**
     * Preprocessor for a single script file that performs minification, header and footer removal, and script whitelisting
     */
    public class ScriptPreprocessor: CSharpSyntaxVisitor {
        public SyntaxTree Tree { get; private set; }
        
        /**
         * Create a new `ScriptPreprocessor` with an empty `Tree` property
         */
        public ScriptPreprocessor() {
            Tree = SyntaxTree(NamespaceDeclaration());; 
        }

        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node) {
            if(node.Name.ToString().Equals("IngameScript")) {
            
            } else {
                throw new Exception("Namespace declared with name != IngameScript");
            }
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node) {
            if(node.Identifier.ToString().Equals("Program") && node.Parent is NamespaceDeclarationSyntax) {

            }
        }
    }
}
