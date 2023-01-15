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
    public enum OperatingMode {
        Station,
        Drone
    }

    partial class Program: MyGridProgram {        
        OperatingMode _mode;
        Logger _log;
        MyIni _ini;
        GyroController _gyro;
        IMyShipController _rc;
        Dictionary<string, Action> _commands = new Dictionary<string, Action>();

        MethodProcess _gyroAlign;
        IProcess _rx;
        IProcess _tx;

        public Program() {
            _log = new Logger(Me.GetSurface(0));

            _ini = new MyIni();
            MyIniParseResult parseResult;
            if(!_ini.TryParse(Me.CustomData, out parseResult)) {
                _log.Panic($"parse conf. @ line {parseResult.LineNo}");
            }
            
            string opMode = _ini.Get("config", "mode").ToString();
            switch(opMode) {
                case "sta": _mode = OperatingMode.Station; break;
                case "dro": _mode = OperatingMode.Drone; break;
                default: _log.Panic($"Invalid or missing config.mode key {opMode}"); break;
            }
            
            List<IMyGyro> controlGyros = new List<IMyGyro>();
            if(_mode == OperatingMode.Drone) GridTerminalSystem.GetBlocksOfType(controlGyros);
            _gyroAlign = new MethodProcess(GyroProcess);
            
            if(_mode == OperatingMode.Drone) {
                _rc = GridTerminalSystem.GetBlockWithName("CONTROL") as IMyShipController;
                _gyro = new GyroController(controlGyros, _rc);
                var comms = new DroneCommsProcess(_log, IGC, _ini);
                _rx = comms.RxProcess;
            } else {
                var comms = new StationCommsProcess(_log, IGC, _ini);
                _rx = comms.RxProc;
                _tx = comms.TxProc;
                Runtime.UpdateFrequency |= UpdateFrequency.Update100;
            }
        }

        public void Save() {
            
        }

        public void Main(string argument, UpdateType updateSource) {
            if(updateSource.HasFlag(UpdateType.Once)) {
                bool done = _gyroAlign.Poll();
                if(!done) {
                    Runtime.UpdateFrequency |= UpdateFrequency.Once;
                }
            } else if(updateSource.HasFlag(UpdateType.Update100)) {

            } else if((updateSource & (UpdateType.Terminal | UpdateType.Script | UpdateType.Trigger)) != 0) {
                
            } else if(updateSource.HasFlag(UpdateType.IGC)) {
                _rx.Poll();
            }
        }

        IEnumerator<Void> GyroProcess() {
            try {
                _gyro.Enable();
                for(;;) {
                    _gyro.Step();
                    yield return Void._;
                }
            } finally {
                _gyro.Disable();
            }
        }
    }
}
