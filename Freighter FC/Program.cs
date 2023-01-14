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
        IMySystem retro_burn;

        public Program() {
            cockpit = GridTerminalSystem.GetBlockWithName("COCKPIT") as IMyShipController;
            orient = new OrientationSystem(GridTerminalSystem);
            stop = new StopSystem(GridTerminalSystem) { parent = this };
            retro_burn = orient.Then(stop);
            orient.gyro.OrientWorld = -cockpit.GetShipVelocities().LinearVelocity;
        }

        public void Save() {
            
        }

        public void Main(string argument, UpdateType updateSource) {
            if(updateSource.HasFlag(UpdateType.Once)) {
                bool done = orient.Poll() && stop.Poll() && retro_burn.Poll();
                if(!done) { Runtime.UpdateFrequency |= UpdateFrequency.Once; }
                return;
            } else if((updateSource & (UpdateType.Terminal | UpdateType.Script | UpdateType.Trigger | UpdateType.Mod)) != 0) {
                if(argument == "align") {
                    orient.gyro.OrientWorld = -cockpit.GetShipVelocities().LinearVelocity;
                    orient.Begin(); 
                } else if(argument == "retro") {
                    orient.gyro.OrientWorld = -cockpit.GetShipVelocities().LinearVelocity;
                    retro_burn.Begin(); 
                }
                Runtime.UpdateFrequency |= UpdateFrequency.Once;
            }
        }

    }
}
