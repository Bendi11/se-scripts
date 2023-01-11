using CommandLine;
namespace SeBuild;

internal class Program {
    [Verb("build", HelpText="build a script project into an output file")]
    public class BuildArgs {
        [Value(0, Required=true, MetaName="script", HelpText="Path to script project's folder")]
        public string Script { get; set; }

        [Option('o', "output", Required=false, HelpText="Path to write a compressed output file")]
        public string? Output { get; set; }
    }

    static void Main(string[] args) {
        Parser.Default.ParseArguments<BuildArgs>(args)
            .MapResult(
                (BuildArgs build) => {
                    return 0;
                },
                (errs) => {
                    foreach(var err in errs) {
                        Console.WriteLine(err);
                    }
                    return 1;
                }
            );
    }
    
}
