using System.Collections.Generic;
using System.Collections;

public struct Nil { public readonly static Nil _; }

public static class Process {
    public static double Time = 0;
    static List<IEnumerator> _procs = new List<IEnumerator>();

    public static void RunMain(double timeStep) {
        for(int i = 0; i < _procs.Count; ++i) {
            bool more = _procs[i].MoveNext();
            if(!more) { _procs[i] = null; }
        }
    }

    public static void Spawn(IEnumerable process) {
        _procs.Add(process.GetEnumerator());
    }

    public static void Spawn(IEnumerator process) {
        _procs.Add(process);
    }

    public static T Get<T>(IEnumerable<T> proc) => proc.GetEnumerator().Current;
}
