﻿using CommandLine;
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
                    var ctx = await ScriptBuilder.Create(build);

                    var syntax = await ctx.BuildProject();
                    
                    string path;
                    
                    if(build.Output == null) {
                        path = Path.Combine(ctx.GameScriptDir, build.Project);
                        Directory.CreateDirectory(path);
                        path = Path.Combine(path, "Script.cs");
                    } else {
                        path = build.Output;
                    }
                    
                    StringBuilder sb = new StringBuilder();
                    foreach(var decl in syntax) { sb.Append(decl.ToFullString()); }
                    
                    string output = sb.ToString();
                    using var file = new StreamWriter(File.Create(path));

                    long len = 0;
                    if(build.Minify) {
                        foreach(var tok in Minifier.Minify(output)) { file.Write(tok); len += tok.Length; }
                    } else {
                        len = output.Length;
                        file.Write(output);
                    }
                    
                    sw.Stop();

 
                    Console.Write($"{path} -");

                    var reduction = ((double)ctx.InitialChars - (double)len) / (double)ctx.InitialChars;
                    var color = reduction switch {
                        <= 0.1 => ConsoleColor.DarkGray,
                        <= 0.5 => ConsoleColor.DarkYellow,
                        <= 0.6 => ConsoleColor.Yellow,
                        <= 0.8 => ConsoleColor.DarkGreen,
                        var _ => ConsoleColor.Green,
                    };

                    Console.Write($" ({len:0,0} characters - ");
                    Console.ForegroundColor = color;
                    Console.Write($"{reduction * 100.0:0.00}");
                    Console.ResetColor();
                    Console.WriteLine($"%)({sw.Elapsed.TotalSeconds:0.000} s)");
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
