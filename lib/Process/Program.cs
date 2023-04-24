using System.Collections.Generic;
using System.Collections;
using System;

namespace IngameScript {
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

        public static T Get<T>(IEnumerable<T> proc) where T: class {
            var current = proc.GetEnumerator().Current;
            return current != default(T) && current is T ? current : default(T);
        }
    }
}
