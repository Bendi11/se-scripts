using System.Collections.Generic;
using System.Collections;


public static class Process {
    public static double Time = 0;
    static List<IEnumerator<int>> _procs = new List<IEnumerator<int>>();

    public static void RunMain(double timeStep) {
        Time += timeStep;
        for(int i = 0; i < _procs.Count; ++i) {
            bool more = _procs[i].MoveNext();
            if(!more) { _procs[i] = null; }
        }
    }

    public static void Spawn(IEnumerable<int> process) {
        _procs.Add(process.GetEnumerator());
    }

    public static void Spawn(IEnumerator<int> process) {
        _procs.Add(process);
    }

    public static T Get<T>(IEnumerable<T> proc) => proc.GetEnumerator().Current;
}
