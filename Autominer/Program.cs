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
        MyIni _ini;

        IProcess _periodic;
        CommsBase _comms;

        public Program() {
            try {
                Log.Init(Me.GetSurface(0));

                _ini = new MyIni();
                MyIniParseResult parseResult;
                if(!_ini.TryParse(Me.CustomData, out parseResult)) {
                    Log.Panic($"parse conf. @ line {parseResult.LineNo}");
                }
                
                string opMode = _ini.Get("config", "mode").ToString();
                switch(opMode) {
                    case "sta": _mode = OperatingMode.Station; break;
                    case "dro": _mode = OperatingMode.Drone; break;
                    default: Log.Panic($"Invalid or missing config.mode key {opMode}"); break;
                }

                if(_mode == OperatingMode.Drone) {
                    var c = new Drone(_ini, IGC, GridTerminalSystem);
                    _periodic = c.Periodic;
                    _periodic.Begin();
                    _comms = c;
                    Runtime.UpdateFrequency |= UpdateFrequency.Update10;
                    Log.Put("init drone complete");
                } else {
                    _comms = new Station(_ini, IGC, GridTerminalSystem);
                    Log.Put("init sta complete");
                }

                _comms.Sendy.TicksPerPeriod = 100;
                _comms.Sendy.RecvProcess.Begin();
                _comms.Sendy.PeriodicProcess.Begin();
                Runtime.UpdateFrequency |= UpdateFrequency.Update100;
            } catch(Exception e) { Log.Panic(e.ToString()); } 
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
            } catch(Exception e) { Log.Panic(e.ToString()); }
        }
    }
}
