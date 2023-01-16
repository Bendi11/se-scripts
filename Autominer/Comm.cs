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
        protected Logger _log;
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
            MOVEDONE = DOMAIN + ".mv",
            ORIENTDONE = DOMAIN + ".or",
            TANKSFULL = REPORT + ".full";

        public CommsBase(Logger log, MyIni ini) {
            _log = log;
            string str;
            if(!ini.Get("auth", "key").TryGetString(out str) || str.Length != 16) {
                _log.Panic("Config option 'auth.key' is not a string or does not contain exactly 16 chars");
            }
            
            if(!TryParseKey(str, AUTHKEY)) {
                _log.Panic("Failed to parse auth.key hex value");
            }
        }
        
        /// <summary>
        /// Attempt to parse a hex string and assign to the given byte array
        /// </summary>
        /// <returns>true if parsing succeeded</returns>
        protected bool TryParseKey(string key, byte[] output) {
            if(key.Length != output.Length * 2) {
                _log.Error($"authkey str len != authkey len");
                return false;
            }
            
            try {
                for(int i = 0; i < key.Length; i += 2) {
                    output[i / 2] = Convert.ToByte(key.Substring(i, 2), 16);
                }
            } catch(Exception e) {
                _log.Error($"authkey parse fail {e.Message}");
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

        public Station(Logger log, MyIni ini, IMyIntergridCommunicationSystem IGC, IMyGridTerminalSystem GTS) : base(log, ini) {
            GTS.GetBlocksOfType(_connectors, conn => MyIni.HasSection(conn.CustomData, "minedock"));
            var dispatch = new Dictionary<string, Sendy.IDispatch>() {
                {
                    TANKSFULL,
                    new EmptyDispatch(conn => {
                        var empty = _connectors.SingleOrDefault(conct => conct.Status == MyShipConnectorStatus.Unconnected);
                        if(empty != null) {
                            conn.Send(ORIENT, empty.WorldMatrix.Forward);
                        } else {
                            _log.Warn($"{conn.Node} no dock: connectors full");
                        }
                    })
                },
                {
                    ORIENTDONE,
                    new EmptyDispatch(conn => {
                        
                    })
                }
            };
            Sendy = new Sendy(_log, IGC, DOMAIN, dispatch) {
                ListenForBroadcast = true,
            };
        }
    }

    class Drone: CommsBase {
        GyroController _gyro;
        IMyRemoteControl _rc;
        IMyShipConnector _connector;
        public IProcess Periodic;
        Sendy.Connection _conn;
        bool orient;
        bool move;

        public Drone(Logger log, MyIni ini, IMyIntergridCommunicationSystem IGC, IMyGridTerminalSystem GTS) : base(log, ini) {
            _rc = GTS.GetBlockWithName("CONTROL") as IMyRemoteControl;
            _rc.SetAutoPilotEnabled(false);
            _connector = GTS.GetBlockWithName("CONNECTOR") as IMyShipConnector;
            List<IMyGyro> controlGyros = new List<IMyGyro>();
            GTS.GetBlocksOfType(controlGyros);
            _gyro = new GyroController(controlGyros, _connector);
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

            Sendy = new Sendy(_log, IGC, DOMAIN, dispatch) {
                TransmitBroadcast = true,
                OnConnection = (conn) => {
                    _log.Log($"conn sta @ {conn.Node}");
                    conn.OnDrop = (_) => conn.Sendy.TransmitBroadcast = true;
                    conn.Sendy.TransmitBroadcast = false;
                    _conn = conn;
                    _conn.Send(TANKSFULL);
                },
            };
        }

        void MoveTo(Vector3D pos) {
            _rc.ClearWaypoints();
            _rc.AddWaypoint(pos, "t");
            _rc.SetAutoPilotEnabled(true);
            _rc.SetCollisionAvoidance(true);
            move = true;
        }

        void Orient(Vector3D axis) {
            _gyro.Enable();
            _gyro.OrientWorld = axis;
            orient = true;
        }

        private IEnumerator<Nil> RunLoop() {
            for(;;) {
                if(orient) {
                    _gyro.Step();
                    if(_gyro.IsOriented) {
                        orient = false;
                        _gyro.Disable();
                        _conn.Send(ORIENTDONE);
                    } 
                }
                yield return Nil._;
            }
        }
    }
}
