using CommandLine;
using CSharpMinifier;
using System.Text;
namespace SeBuild;

internal class Program {
    static async Task Main(string[] args) {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await Parser
            .Default
            .ParseArguments<BuildArgs>(args)
            .MapResult(
                async (BuildArgs build) => {
                    string projectName = build.Project ?? throw new ArgumentNullException();
                    var ctx = await ScriptBuilder.Create(build);

                    var syntax = await ctx.BuildProject();
                    
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
