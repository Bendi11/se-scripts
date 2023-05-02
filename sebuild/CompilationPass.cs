using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace SeBuild;

public sealed class ScriptCommon {
    public Solution Solution;
    public ProjectId ProjectId;
    public List<DocumentId> Documents;
    public BuildArgs Args;

    public Project Project {
        get => Solution.GetProject(ProjectId)!;
    }

    public IEnumerable<Document> DocumentsIter {
        get {
            foreach(var id in Documents) {
                yield return Solution.GetDocument(id)!;
            }
        }
    }

    public ScriptCommon(Solution sln, ProjectId project, List<DocumentId> docs, BuildArgs args) {
        Solution = sln;
        ProjectId = project;
        Documents = docs;
        Args = args;
    }
}

public class PassProgress: IProgress<int>, IDisposable {
    string _name;
    public string? Message = null;
    int _total = 0;
    Stopwatch _stopWatch;

    public PassProgress(string name) {
        _name = name;
        _stopWatch = new Stopwatch();
        _stopWatch.Start();
    }

    static readonly char[] TICKER = {'▉', '▊', '▋', '▌', '▍', '▎', '▏', '▎', '▍', '▌', '▋', '▊', '▉'};
    static readonly string CLEAR = new string(' ', Console.WindowWidth);

    void ClearLine() {
        Console.SetCursorPosition(0, Console.GetCursorPosition().Top);
        Console.Write(CLEAR);
        Console.SetCursorPosition(0, Console.GetCursorPosition().Top);
    }

    public void Report(int value) {
        _total += value;
        Console.CursorVisible = false;
        ClearLine();
        Console.Write($"{_name}: [{_total:0,0}] {TICKER[_total % TICKER.Count()]}{(Message is null ? "" : $" - {Message}")}\r");
    }

    public void Dispose() {
        _stopWatch.Stop();
        var old = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;

        ClearLine();
        Console.WriteLine($"\r✓ {_name} - {_stopWatch.Elapsed.TotalSeconds:0.000}");

        Console.CursorVisible = true;
        Console.ForegroundColor = old;
    }
}

public abstract class CompilationPass {
    public ScriptCommon Common;
    public PassProgress? Progress = null;

    protected void Tick() {
        if(Progress is not null) {
            Progress.Report(1);
        }
    }

    protected void Msg(string message) {
        if(Progress is not null) {
            Progress.Message = message;
        }
    }

    public CompilationPass(ScriptCommon ctx) {
        Common = ctx;
    }
    
    /// Execute the pass on the loaded documents, potentially replacing `Solution` with a new solution
    public abstract Task Execute();
}
