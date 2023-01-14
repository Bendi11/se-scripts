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
        GyroController gyro;
        IMyShipController rc;

        public Program() {
            List<IMyGyro> controlGyros = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType(controlGyros);

            rc = GridTerminalSystem.GetBlockWithName("CONTROLLER") as IMyShipController;
            gyro = new GyroController(controlGyros, rc);
            Runtime.UpdateFrequency |= UpdateFrequency.Update10;
        }

        public void Save() {
            
        }

        public void Main(string argument, UpdateType updateSource) {
            if(updateSource.HasFlag(UpdateType.Trigger) && argument.Equals("align")) {
                gyro.OrientLocal = Vector3.Up;
                foreach(var g in gyro.Gyros) {
                    g.GyroOverride = true;
                }
            }
            Echo(""+gyro.Step());
        }
    }
}
