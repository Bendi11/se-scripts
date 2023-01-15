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
        Sendy _sendy;

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

            var comms = new CommsBase(_log, _ini);
            
            if(_mode == OperatingMode.Drone) {
                _rc = GridTerminalSystem.GetBlockWithName("CONTROL") as IMyShipController;
                List<IMyGyro> controlGyros = new List<IMyGyro>();
                GridTerminalSystem.GetBlocksOfType(controlGyros);
                _gyro = new GyroController(controlGyros, _rc);
                _gyroAlign = new MethodProcess(GyroProcess);
                _sendy = comms.Drone(IGC);
                _log.Log("init drone complete");
            } else {
                _sendy = comms.Station(IGC);
                _log.Log("init sta complete");
            }

            _sendy.TicksPerPeriod = 100;
            _sendy.RecvProcess.Begin();
            _sendy.PeriodicProcess.Begin();
            Runtime.UpdateFrequency |= UpdateFrequency.Update100;
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
                _sendy.PeriodicProcess.Poll();
            } else if((updateSource & (UpdateType.Terminal | UpdateType.Script | UpdateType.Trigger)) != 0) {
                
            } else if(updateSource.HasFlag(UpdateType.IGC)) {
                _sendy.RecvProcess.Poll();
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
