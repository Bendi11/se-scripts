using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;
using System.Collections.Immutable;

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

            public IEnumerator<object> GetEnumerator() { return Progress; }

            public void Begin() { Progress = Run(); }

            protected abstract IEnumerator<object> Run();

            public bool Poll() {
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
