using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System;
using VRage.Game.ModAPI.Ingame.Utilities;

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
            
            if(_mode == OperatingMode.Drone) {
                _rc = GridTerminalSystem.GetBlockWithName("CONTROL") as IMyShipController;
                List<IMyGyro> controlGyros = new List<IMyGyro>();
                GridTerminalSystem.GetBlocksOfType(controlGyros);
                _gyro = new GyroController(controlGyros, _rc);
                _gyroAlign = new MethodProcess(GyroProcess);
                var comms = new DroneCommsProcess(_log, IGC, _ini);
                _rx = comms.RxProcess;
                _rx.Begin();
                _log.Log("init drone complete");
            } else {
                var comms = new StationCommsProcess(_log, IGC, _ini);
                _rx = comms.RxProc;
                _tx = comms.TxProc;
                _rx.Begin();
                _tx.Begin();
                Runtime.UpdateFrequency |= UpdateFrequency.Update100;
                _log.Log("init sta complete");
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
                _tx.Poll();
            } else if((updateSource & (UpdateType.Terminal | UpdateType.Script | UpdateType.Trigger)) != 0) {
                
            } else if(updateSource.HasFlag(UpdateType.IGC)) {
                _rx.Poll();
            }
        }

        IEnumerator<Nil> GyroProcess() {
            try {
                _gyro.Enable();
                for(;;) {
                    _gyro.Step();
                    yield return Nil._;
                }
            } finally {
                _gyro.Disable();
            }
        }
    }
}
