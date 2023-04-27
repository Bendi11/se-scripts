using System.Collections.Generic;
using System.Collections;

namespace IngameScript {
    public struct Nil { public readonly static Nil _; }

    public static class Process {
        public static double Time = 0;
        static List<IEnumerable> _procs = new List<IEnumerable>();

        public static void RunMain(double timeStep) {
            for(int i = 0; i < _procs.Count; ++i) {
                bool more = _procs[i].GetEnumerator().MoveNext();
                if(!more) { _procs[i] = null; }
            }
        }

        public static void Spawn(IEnumerable process) {
            _procs.Add(process);
        }

        public static T Get<T>(IEnumerable<T> proc) => proc.GetEnumerator().Current;
    }
}
