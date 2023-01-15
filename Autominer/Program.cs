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
using System.Collections;

namespace IngameScript {
    partial class Program: MyGridProgram {
        Logger _log;
        GyroController _gyro;
        IMyShipController _rc;
        Dictionary<string, Action> _commands = new Dictionary<string, Action>();

        MethodProcess antennaRecv;

        public Program() {
            List<IMyGyro> controlGyros = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType(controlGyros);
            
            _log = new Logger(Me.GetSurface(0));
            _rc = GridTerminalSystem.GetBlockWithName("CONTROLLER") as IMyShipController;
            _gyro = new GyroController(controlGyros, _rc);
        }

        public void Save() {
            
        }

        public void Main(string argument, UpdateType updateSource) {
            if(updateSource.HasFlag(UpdateType.Trigger) && argument.Equals("align")) {
                _gyro.OrientLocal = Vector3.Up;
                foreach(var g in _gyro.Gyros) {
                    g.GyroOverride = true;
                }
            } else if(updateSource.HasFlag(UpdateType.IGC)) {
                
            }

            _log.Log("" + _gyro.Step());
        }

        IEnumerator<IProcess.Void> GyroProcess() {
            try {

            } finally {
            }
        }
    }
}
