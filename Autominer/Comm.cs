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
            // MyTuple<Vector3, Vector3>: pos, world orient
            RETURN = CMD + ".ret",
            HOLD = CMD + ".hold",
            REPORT = DOMAIN + "rep",
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
        List<IMyShipConnector> _connectors;

        public Station(Logger log, MyIni ini, IMyIntergridCommunicationSystem IGC, IMyGridTerminalSystem GTS) : base(log, ini) {
            GTS.GetBlocksOfType(_connectors, conn => MyIni.HasSection(conn.CustomData, "minedock"));
            var dispatch = new Dictionary<string, Sendy.IDispatch>() {
                {
                    TANKSFULL,
                    new Dispatch<int>((conn, _) => {
                        
                    })
                }
            };
            Sendy = new Sendy(_log, IGC, DOMAIN, dispatch) {
                ListenForBroadcast = true
            };
        }
    }

    class Drone: CommsBase {
        GyroController _gyro;
        IMyRemoteControl _rc;
        public IProcess Periodic;

        public Drone(Logger log, MyIni ini, IMyIntergridCommunicationSystem IGC, IMyGridTerminalSystem GTS) : base(log, ini) {
            _rc = GTS.GetBlockWithName("CONTROL") as IMyRemoteControl;
            List<IMyGyro> controlGyros = new List<IMyGyro>();
            GTS.GetBlocksOfType(controlGyros);
            _gyro = new GyroController(controlGyros, _rc);
            Periodic = new MethodProcess(OrientProcess, () => _gyro.Enable(), () => _gyro.Disable());

            var dispatch = new Dictionary<string, Sendy.IDispatch>() {
                {
                    RETURN,
                    new Dispatch<MyTuple<Vector3, Vector3>>((conn, data) => {
                         
                    })
                }
            };
            Sendy = new Sendy(_log, IGC, DOMAIN, dispatch) {
                TransmitBroadcast = true,
                OnConnection = (conn) => {
                    _log.Log($"conn sta @ {conn.Node}");
                    conn.OnDrop = (_) => conn.Sendy.TransmitBroadcast = true;
                    conn.Sendy.TransmitBroadcast = false;
                },
            };
        }

        private IEnumerator<Nil> OrientProcess() {
            for(;;) {
                _gyro.Step();
                yield return Nil._;
            }
        }
    }
}
