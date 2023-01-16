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
            MOVE = CMD + ".mv",
            // Vector3D orient
            ORIENT = CMD + ".or",
            HOLD = CMD + ".hold",

            REPORT = DOMAIN + ".rep",
            MOVEDONE = REPORT + ".mv",
            ORIENTDONE = REPORT + ".or",
            TANKSFULL = REPORT + ".full";

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
    }

    class Station: CommsBase {
        List<IMyShipConnector> _connectors = new List<IMyShipConnector>();

        class DroneConn: Sendy.Connection {
            public IMyShipConnector Assigned = null;
            public DroneConn(Sendy s, long a) : base(s, a) {}
        }

        public Station(MyIni ini, IMyIntergridCommunicationSystem IGC, IMyGridTerminalSystem GTS) : base(ini) {
            GTS.GetBlocksOfType(_connectors, conn => MyIni.HasSection(conn.CustomData, "minedock"));
            var dispatch = new Dictionary<string, Sendy.IDispatch>() {
                {
                    TANKSFULL,
                    new EmptyDispatch<DroneConn>(conn => {
                        if(AssignPort(conn)) {
                            var mat = conn.Assigned.WorldMatrix;
                            conn.Send(MOVE, mat.Translation + mat.GetOrientation().Forward * 5);
                        }
                    })
                },
                {
                    ORIENTDONE,
                    new EmptyDispatch<DroneConn>(conn => {
                        if(AssignPort(conn)) {
                        
                        }
                    })
                },
                {
                    MOVEDONE,
                    new EmptyDispatch<DroneConn>(conn => {
                        if(AssignPort(conn)) {
                            conn.Send(ORIENT, conn.Assigned.WorldMatrix.GetOrientation().Backward);
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
        public IProcess Periodic;
        StationConn _conn;
        bool _orient;

        bool _move;
        Vector3D _pos;

        class StationConn: Sendy.Connection {
            public StationConn(Sendy s, long a) : base(s, a) {
                Sendy.TransmitBroadcast = false;
                Log.Put($"conn sta @ {Node}");
                Send(TANKSFULL);
            }

            public override void Close() {
                Sendy.TransmitBroadcast = true;
                base.Close();
            }
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
                    MOVE,
                    new Dispatch<Vector3D>((conn, data) => MoveTo(data))
                },
                {
                    ORIENT,
                    new Dispatch<Vector3D>((conn, data) => Orient(data))
                }
            };

            Sendy = new Sendy(IGC, DOMAIN, dispatch) {
                TransmitBroadcast = true,
                CreateConnection = (s, a) => {
                    _conn = new StationConn(s, a);
                    return _conn;
                }
            };
        }

        void MoveTo(Vector3D pos) {
            _pos = pos;
            _ap.PositionWorld = _rc.GetPosition() + Vector3D.Up * 3;
            _ap.Enabled = true;
            _move = true;
        }

        void Orient(Vector3D axis) {
            _gyro.Enable();
            _gyro.OrientWorld = axis;
            _orient = true;
        }

        private IEnumerator<Nil> RunLoop() {
            for(;;) {
                if(_orient) {
                    _gyro.Step();
                    if(_gyro.IsOriented) {
                        _orient = false;
                        _gyro.Disable();
                        _conn.Send(ORIENTDONE);
                    } 
                }

                if(_move) {
                    _ap.Step();
                    if(_rc.GetShipSpeed() < 0.1 && (_rc.GetPosition() - _ap.PositionWorld).Length() < 1) {
                        _move = false;
                        _ap.Enabled = false;
                        _conn.Send(MOVEDONE);
                    }
                }

                yield return Nil._;
            }
        }
    }
}
