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
        IMyShipController cockpit;
        OrientationSystem orient;
        StopSystem stop;

        bool align;
        bool retro;

        public Program() {
            cockpit = GridTerminalSystem.GetBlockWithName("COCKPIT") as IMyShipController;
            orient = new OrientationSystem(GridTerminalSystem);
            stop = new StopSystem(GridTerminalSystem);
            Runtime.UpdateFrequency |= UpdateFrequency.Update10;
        }

        public void Save() {
            
        }

        public void Main(string argument, UpdateType updateSource) {
            if(updateSource.HasFlag(UpdateType.Once)) {
                
            } else if((updateSource & (UpdateType.Terminal | UpdateType.Script | UpdateType.Trigger | UpdateType.Mod)) != 0) {
                if(argument == "align" && !retro) {
                    align = !align;
                } else if(argument == "retro" && !align) {
                    retro = !retro;
                }
            }
        }
    }
}
