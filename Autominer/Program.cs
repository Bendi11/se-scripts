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

        IProcess _periodic;
        CommsBase _comms;

        public Program() {
            try {
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
                    var c = new Drone(_log, _ini, IGC, GridTerminalSystem);
                    _periodic = c.Periodic;
                    _comms = c;
                    Runtime.UpdateFrequency |= UpdateFrequency.Update10;
                    _log.Log("init drone complete");
                } else {
                    _comms = new Station(_log, _ini, IGC, GridTerminalSystem);
                    _log.Log("init sta complete");
                }

                _comms.Sendy.TicksPerPeriod = 100;
                _comms.Sendy.RecvProcess.Begin();
                _comms.Sendy.PeriodicProcess.Begin();
                Runtime.UpdateFrequency |= UpdateFrequency.Update100;
            } catch(Exception e) { _log.Panic(e.Message); }
        }

        public void Save() {
            
        }

        public void Main(string argument, UpdateType updateSource) {
            try {
                if(updateSource.HasFlag(UpdateType.Update10)) {
                    _periodic.Poll();
                } else if(updateSource.HasFlag(UpdateType.Update100)) {
                    _comms.Sendy.PeriodicProcess.Poll();
                } else if((updateSource & (UpdateType.Terminal | UpdateType.Script | UpdateType.Trigger)) != 0) {
                    
                } else if(updateSource.HasFlag(UpdateType.IGC)) {
                    _comms.Sendy.RecvProcess.Poll();
                }
            } catch(Exception e) { _log.Panic(e.Message); }
        }
    }
}
