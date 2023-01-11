using CSharpMinifier;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SeBuild;

public class ScriptWorkspaceContext {
    MSBuildWorkspace workspace;
    Solution? sln = null;
    
    /// <summary>Create a new `ScriptWorkspaceContext`</summary>
    /// <param name="path">
    /// If <c>path</c> is a file, then attempt to open a solution from the given file
    /// Otherwise, search through the directory that <c>path</c> points to to find a .sln file
    /// </param>
    static async public Task<ScriptWorkspaceContext> Create(string path) {
        var me = new ScriptWorkspaceContext();
        await me.Init(path);
        return me;
    }

    private ScriptWorkspaceContext() {
        workspace = MSBuildWorkspace.Create();
    }

    async private Task Init(string path) {
        string slnPath = path;
        if(!File.Exists(path)) {
            foreach(var file in Directory.GetFiles(path)) {
                if(Path.GetExtension(file).ToUpper().Equals(".SLN")) {
                    slnPath = file;
                    break;
                }
            }
        }

        sln = await workspace.OpenSolutionAsync(slnPath);
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
            Tree = SyntaxTree(NamespaceDeclaration());
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

/// <summary>
/// Builds a single .csproj project: resolves project references, removes header / footer code, minifies, and writes the
/// final script output to a file
/// </summary>
public class ProjectBuilder {
    public readonly Project Project;
    private HashSet<Project> loaded;
    NamespaceDeclarationSyntax ns;

    public ProjectBuilder(Project proj, NamespaceDeclarationSyntax? name = null) {
        this.Project = proj;
        loaded = new HashSet<Project>();
        ns = name ?? NamespaceDeclaration(IdentifierName(""));
    }
    
    private class DocumentWalker: CSharpSyntaxWalker {
        NamespaceDeclarationSyntax ns;

        public DocumentWalker(NamespaceDeclarationSyntax name) {
            ns = name;
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node) => ns.AddMembers(node);
        public override void VisitGlobalStatement(GlobalStatementSyntax node) => ns.AddMembers(node);
        public override void VisitEnumDeclaration(EnumDeclarationSyntax node) => ns.AddMembers(node);
        public override void VisitStructDeclaration(StructDeclarationSyntax node) => ns.AddMembers(node);
        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node) => ns.AddMembers(node);
    }

    async private Task Digest(Document doc) {
        var syntax = await doc.GetSyntaxTreeAsync();
        if(syntax is not CSharpSyntaxTree) { throw new Exception("Cannot compile non-C# files"); }
        var csSyntax = (CSharpSyntaxTree)syntax;
        
        csSyntax.GetRoot().Accept(new DocumentWalker(ns));
    }
}
