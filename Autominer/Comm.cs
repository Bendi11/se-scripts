using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Collections.Immutable;
using VRageMath;
using VRage;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript {
    class CommsBase {
        public Sendy Sendy;
        protected readonly byte[] AUTHKEY = new byte[8];
        protected byte[] PARSE_AUTHKEY = new byte[8];

        public const string
            DOMAIN = "fzy.am",

            CMD = DOMAIN + ".cmd",
            // Vector3D pos
            STAGEDOCK = CMD + ".stage",
            // Vector3D orient
            ORIENTDOCK = CMD + ".orient",
            DOCK = CMD + ".dock",
            HOLD = CMD + ".hold",

            REPORT = DOMAIN + ".rep"; 

        public CommsBase(MyIni ini) {
            string str;
            if(!ini.Get("auth", "key").TryGetString(out str) || str.Length != 16) {
                Log.Panic("Config option 'auth.key' is not a string or does not contain exactly 16 chars");
            }
            
            if(!TryParseKey(str, AUTHKEY)) {
                Log.Panic("Failed to parse auth.key hex value");
            }
        }
        
        /// <summary>
        /// Attempt to parse a hex string and assign to the given byte array
        /// </summary>
        /// <returns>true if parsing succeeded</returns>
        protected bool TryParseKey(string key, byte[] output) {
            if(key.Length != output.Length * 2) {
                Log.Error($"authkey str len != authkey len");
                return false;
            }
            
            try {
                for(int i = 0; i < key.Length; i += 2) {
                    output[i / 2] = Convert.ToByte(key.Substring(i, 2), 16);
                }
            } catch(Exception e) {
                Log.Error($"authkey parse fail {e.Message}");
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// Use a simple XOR cipher to encode the given byte array for transport
        /// </summary>
        protected void Code(byte[] output) {
            for(byte i = 0; i < output.Length; ++i) {
                output[i] ^= 0x3E;
            }
        }

        [Flags]
        protected enum State {
            Unknown = 1,
            Transitioning = 2,
            Docked = 4,
            StageDock = 8,
            OrientedDock = 16,
            Hold = 32
        }
    }

    class Station: CommsBase {
        List<IMyShipConnector> _connectors = new List<IMyShipConnector>();

        class DroneConn: Sendy.Connection {
            public State ReportedState = State.Unknown;
            public IMyShipConnector Assigned = null;
            public DroneConn(Sendy s, long a) : base(s, a) {}
        }

        public Station(MyIni ini, IMyIntergridCommunicationSystem IGC, IMyGridTerminalSystem GTS) : base(ini) {
            GTS.GetBlocksOfType(_connectors, conn => MyIni.HasSection(conn.CustomData, "minedock"));
            var dispatch = new Dictionary<string, Sendy.IDispatch>() {
                {
                    REPORT,
                    new Dispatch<string, DroneConn>((conn, str) => {
                        try {
                            conn.ReportedState = (State)Enum.Parse(typeof(State), str);
                            switch(conn.ReportedState) {
                                case State.Unknown: 
                                    if(AssignPort(conn)) {
                                        var mat = conn.Assigned.WorldMatrix;
                                        conn.Send(STAGEDOCK, mat.Translation + mat.Forward * 25);
                                    }
                                break;
                                case State.OrientedDock: conn.Send(DOCK, conn.Assigned.GetPosition()); break;
                                case State.StageDock: conn.Send(ORIENTDOCK, conn.Assigned.WorldMatrix.GetOrientation().Backward); break;
                            } 
                        } catch(Exception e) {
                            Log.Error($"Failed to parse reported state: {e.Message}");
                        }
                    })
                }
            };
            Sendy = new Sendy(IGC, DOMAIN, dispatch) {
                ListenForBroadcast = true,
                CreateConnection = (a, b) => new DroneConn(a, b)
            };
        }

        private bool AssignPort(DroneConn conn) {
            if(conn.Assigned != null) { return true; }
            var empty = _connectors.SingleOrDefault(conct => conct.Status == MyShipConnectorStatus.Unconnected);
            if(empty != null) {
                conn.Assigned = empty;
            } else {
                Log.Warn($"{conn.Node} no dock: connectors full");
            }

            return conn.Assigned != null;
        }
    }

    class Drone: CommsBase {
        GyroController _gyro;
        Autopilot _ap;
        IMyRemoteControl _rc;
        IMyShipConnector _connector;
        public Process<Nil> Periodic;
        StationConn _conn;
        bool _orient;

        Process<Nil> _proc;

        State _state = State.Unknown;

        bool Valid(State flags) {
            if(_proc != null || (_state & flags) != 0) {
                return true;
            } else {
                Log.Error($"Invalid command for current state");
                _state = State.Unknown;
                return false;
            }
        }

        class StationConn: Sendy.Connection {
            public StationConn(Sendy s, long a) : base(s, a) {
                Sendy.TransmitBroadcast = false;
                Log.Put($"conn sta @ {Node}");
            }

            public override void Close() {
                Sendy.TransmitBroadcast = true;
                base.Close();
            }
        }

        void Report() {
            _conn.Send(REPORT, Enum.GetName(typeof(State), _state));
        }

        void Transition(Process<Nil> proc) {
            if(_proc != null) _proc.Stop();
            _proc = proc;
        }

        public Drone(MyIni ini, IMyIntergridCommunicationSystem IGC, IMyGridTerminalSystem GTS) : base(ini) {
            _rc = GTS.GetBlockWithName("CONTROL") as IMyRemoteControl;
            _ap = new Autopilot(GTS, _rc);
            _ap.SpeedLimit = 7F;
            _connector = GTS.GetBlockWithName("CONNECTOR") as IMyShipConnector;
            List<IMyGyro> controlGyros = new List<IMyGyro>();
            GTS.GetBlocksOfType(controlGyros);
            _gyro = new GyroController(controlGyros, _connector);
            _gyro.OrientedThreshold = 0.01F;
            _gyro.Rate = 0.3F;
            Periodic = new MethodProcess(RunLoop);

            var dispatch = new Dictionary<string, Sendy.IDispatch>() {
                {
                    STAGEDOCK,
                    new Dispatch<Vector3D>((conn, data) => { if(Valid(State.Unknown | State.Hold)) { Transition(new MethodProcess<Nil>(() => StageDock(data))); }})
                },
                {
                    ORIENTDOCK,
                    new Dispatch<Vector3D>((conn, data) => { if(Valid(State.StageDock)) { Transition(new MethodProcess<Nil>(() => Orient(data))); }})
                },
                {
                    DOCK,
                    new Dispatch<Vector3D>((conn, data) => {
                        if(!Valid(State.OrientedDock)) { return; }
                        Transition(new MethodProcess<Nil>(() => Connect(data))); 
                    })
                }
            };

            Sendy = new Sendy(IGC, DOMAIN, dispatch) {
                TransmitBroadcast = true,
                CreateConnection = (s, a) => {
                    _conn = new StationConn(s, a);
                    Report();
                    return _conn;
                }
            };
        }

        IEnumerator<Nil> MoveTo(Vector3D pos) {
            _ap.PositionWorld = pos;
            _ap.Enabled = true;

            try {
                while(_rc.GetShipSpeed() < 0.1 && (_rc.GetPosition() - _ap.PositionWorld).Length() < 0.5) {
                    _ap.Step();
                    yield return Nil._;
                }
            } finally {
                _ap.Enabled = false;
            }
        }

        IEnumerator<Nil> StageDock(Vector3D pos) {
            foreach(var _ in new MethodProcess(() => MoveTo(pos))) { yield return Nil._; }
            _state = State.StageDock;
        }

        IEnumerator<Nil> Orient(Vector3D axis) {
            _gyro.Enable();
            _gyro.OrientWorld = axis;
            
            try {
                while(!_gyro.IsOriented) {
                    _gyro.Step();
                    yield return Nil._;
                }
                _state = State.OrientedDock;
            } finally {
                _orient = false;
                _gyro.Disable();
                _orient = true;
            }
        }

        IEnumerator<Nil> Connect(Vector3D pos) {
            var mv = new MethodProcess(() => MoveTo(pos));
            _ap.Ref = _connector;
 
            try {
                foreach(var _ in mv) {
                    _connector.Connect();
                    if(_connector.Status == MyShipConnectorStatus.Connected) {
                        break;
                    }
                    yield return Nil._;
                }
                
                if(_connector.Status == MyShipConnectorStatus.Connected) {
                    _state = State.Docked;
                } else {
                    _state = State.Unknown;
                }
            } finally {
                _ap.Ref = _rc;
            }
        }

        private IEnumerator<Nil> RunLoop() {
            for(;;) {
                if(_proc != null) {
                    if(_proc.Poll()) {
                        Report();
                        _proc = null;
                    }
                }
                yield return Nil._;
            }
        }
    }
}
