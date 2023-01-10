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
        public abstract class IMySystem<T>  {
            public IEnumerator<T> Progress { get; protected set; }
            public bool InProgress {
                get {
                    return Progress != null;
                }
            }

            public abstract void Begin();

            public bool Poll(out T val) {
                bool more = Progress.MoveNext();

                if(more) {
                    val = default(T);
                    return false;
                } else {
                    val = Progress.Current;
                    Progress.Dispose();
                    Progress = null;
                    return true;
                }
            }
        }
    }
}
