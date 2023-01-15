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
        IMyBroadcastListener _recv;

        MethodProcess _gyroAlign;

        public Program() {
            List<IMyGyro> controlGyros = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType(controlGyros);
            _gyroAlign = new MethodProcess(GyroProcess);
            
            _log = new Logger(Me.GetSurface(0));
            _rc = GridTerminalSystem.GetBlockWithName("CONTROLLER") as IMyShipController;
            _gyro = new GyroController(controlGyros, _rc);
            _recv = IGC.RegisterBroadcastListener("mine");
            _recv.SetMessageCallback();
        }

        public void Save() {
            
        }

        public void Main(string argument, UpdateType updateSource) {
            if(updateSource.HasFlag(UpdateType.Once)) {
                bool done = _gyroAlign.Poll();
                if(!done) {
                    Runtime.UpdateFrequency |= UpdateFrequency.Once;
                }
            } else if((updateSource & (UpdateType.Terminal | UpdateType.Script | UpdateType.Trigger)) != 0) {
                
            } else if(updateSource.HasFlag(UpdateType.IGC)) {
                while(_recv.HasPendingMessage) {
                    var msg = _recv.AcceptMessage();
                }
            }
        }

        IEnumerator<IProcess.Void> GyroProcess() {
            try {
                _gyro.Enable();
                for(;;) {
                    _gyro.Step();
                    yield return IProcess.VOID;
                }
            } finally {
                _gyro.Disable();
            }
        }
    }
}
