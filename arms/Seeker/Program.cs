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
        Seeker seeker;
         
        public Program() {
            Log.Init(Me.GetSurface(0));
            seeker = new Seeker(GridTerminalSystem, Me, Runtime);
            seeker.Seek.Begin();
            Runtime.UpdateFrequency |= UpdateFrequency.Once;
        }

        public void Save() {
            
        }

        public void Main(string argument, UpdateType updateSource) {
            if(updateSource.HasFlag(UpdateType.Once)) {
                if(!seeker.Seek.Poll()) {
                    Runtime.UpdateFrequency |= UpdateFrequency.Once;
                }
            }
        }
    }
}
