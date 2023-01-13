using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    partial class Program: MyGridProgram {
        public abstract class IMySystem  {
            protected IEnumerator<object> _prog;
            public IEnumerator<object> Progress {
                get { return _prog; }
                set {
                    if(_prog != null) {
                        _prog.Dispose();
                    }
                    _prog = value;
                }
            }
            public bool InProgress {
                get {
                    return Progress != null;
                }
            }

            private class ThenSystem: IMySystem {
                public IMySystem a, b;
                
                protected override IEnumerator<object> Run() {
                    a.Begin();
                    foreach(var v in a) { yield return null; }
                    b.Begin();
                    foreach(var v in b) { yield return null; }
                }
            }

            private class AndSystem: IMySystem {
                public IMySystem a, b;

                protected override IEnumerator<object> Run() {
                    a.Begin();
                    b.Begin();

                    while(a.InProgress) {
                        a.Poll();
                        b.Poll();
                        yield return null;
                    }
                }
            }

            /**
             * Execute this system, followed by `next`
             */
            public IMySystem Then(IMySystem next) => new ThenSystem() { a = this, b = next };
            /**
             * Execute this system in parallel with `parallel`, runs until `this` system returns
             */
            public IMySystem And(IMySystem parallel) => new AndSystem() { a = this, b = parallel };

            public IEnumerator<object> GetEnumerator() { return Progress; }

            public void Begin() { Progress = Run(); }

            protected abstract IEnumerator<object> Run();

            public virtual bool Poll() {
                if(Progress == null) { return true; }
                bool more = Progress.MoveNext();

                if(more) {
                    return false;
                } else {
                    Progress.Dispose();
                    Progress = null;
                    return true;
                }
            }
        }

        class ClosureSystem: IMySystem {
            Func<IEnumerator<object>> f;

            public ClosureSystem(Func<IEnumerator<object>> func) { f = func; }
            protected override IEnumerator<object> Run() { return f(); }
        }
    }
}
