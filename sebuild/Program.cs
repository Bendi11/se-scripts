using CommandLine;
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
    }

    static async Task Main(string[] args) {
        await Parser.Default.ParseArguments<BuildArgs>(args)
            .MapResult(
                async (BuildArgs build) => {
                    string projectName = build.Project ?? throw new ArgumentNullException();
                    var ctx = await ScriptWorkspaceContext.Create(build.SolutionPath ?? throw new ArgumentNullException());

                    var syntax = await ctx.BuildProject(projectName);
                    
                    string path;
                    
                    if(build.Output == null) {
                        path = Path.Combine(ctx.GameScriptDir, projectName);
                        Directory.CreateDirectory(path);
                        path = Path.Combine(path, "Script.cs");
                    } else {
                        path = build.Output;
                    }
                    
                    using var file = new StreamWriter(File.OpenWrite(path));
                    syntax.WriteTo(file);
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
