using System.Collections.Generic;
using System;

namespace IngameScript {
    public static class Process {
        public static double Time = 0;
        public static T Get<T>(IEnumerable<T> proc) where T: class {
            var current = proc.GetEnumerator().Current;
            return current != default(T) && current is T ? current : default(T);
        }
    }
}
