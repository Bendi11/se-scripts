using CommandLine;
using CSharpMinifier;
using System.Text;
namespace SeBuild;

internal class Program {
    [Verb("build", HelpText="build a script project into an output file")]
    public class BuildArgs {
        [Option('s', "sln", HelpText = "Path to a solution file or a directory containing one", Default = "./")]
        public string? SolutionPath { get; set; }

        [Value(0, Required=true, MetaName="script", HelpText="Name of the project in solution")]
        public string? Project { get; set; }

        [Option('o', "output", Required=false, HelpText="Path to write a compressed output file")]
        public string? Output { get; set; }

        [Option('m', "minify", Required=false, HelpText = "Minify the produced output")]
        public bool Minify { get; set; }

        [Option('r', "rename", Required = false, HelpText = "Rename symbols to reduce output size further")]
        public bool Rename { get; set; }

        [Option(
            'd',
            "remove-dead",
            Required = false,
            HelpText = "Remove dead code not referenced by the Program class"
        )]
        public bool RemoveDead { get; set; }
    }

    static async Task Main(string[] args) {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await Parser
            .Default
            .ParseArguments<BuildArgs>(args)
            .MapResult(
                async (BuildArgs build) => {
                    string projectName = build.Project ?? throw new ArgumentNullException();
                    var ctx = await ScriptWorkspaceContext.Create(build.SolutionPath ?? throw new ArgumentNullException());

                    var syntax = await ctx.BuildProject(projectName, build.Rename, build.RemoveDead);
                    
                    string path;
                    
                    if(build.Output == null) {
                        path = Path.Combine(ctx.GameScriptDir, projectName);
                        Directory.CreateDirectory(path);
                        path = Path.Combine(path, "Script.cs");
                    } else {
                        path = build.Output;
                    }
                    
                    StringBuilder sb = new StringBuilder();
                    foreach(var decl in syntax) { sb.Append(decl.ToFullString()); }
                    
                    string output = sb.ToString();
                    using var file = new StreamWriter(File.Create(path));
                    
                    Console.Write($"Writing output {path}");
                    long len = 0;
                    if(build.Minify) {
                        foreach(var tok in Minifier.Minify(output)) { file.Write(tok); len += tok.Length; }
                    } else {
                        len = output.Length;
                        file.Write(output);
                    }
                    
                    sw.Stop();
                    Console.WriteLine($" ({len} characters)({sw.Elapsed.TotalSeconds:0.00} s)");
                },
                async (errs) => await Task.Run(() => {
                    foreach(var err in errs) {
                        Console.WriteLine(err);
                    }
                    
                    return 1;
                })
            );
    }
}
