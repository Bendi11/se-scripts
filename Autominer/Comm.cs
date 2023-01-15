using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript {
    class CommsBase {
        protected Logger _log;
        protected IMyIntergridCommunicationSystem _IGC;
        protected readonly byte[] AUTHKEY = new byte[8];
        protected byte[] PARSE_AUTHKEY = new byte[8];

        public CommsBase(Logger log, IMyIntergridCommunicationSystem IGC, MyIni ini) {
            _log = log;
            _IGC = IGC;
            string str;
            if(!ini.Get("auth", "key").TryGetString(out str) || str.Length != 16) {
                _log.Panic("Config option 'auth.key' is not a string or does not contain exactly 16 chars");
            }
        }
        
        /// <summary>
        /// Attempt to parse a hex string and assign to the given byte array
        /// </summary>
        /// <returns>true if parsing succeeded</returns>
        protected bool TryParseKey(string key, ref byte[] output) {
            if(key.Length * 2 != output.Length) {
                _log.Error($"authkey str len != authkey len");
                return false;
            }
            
            try {
                for(int i = 0; i < output.Length; ++i) {
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
        protected void Code(ref byte[] output) {
            for(byte i = 0; i < output.Length; ++i) {
                output[i] ^= (byte)(i * 0x2F);
            }
        }
    }

    class DroneCommsProcess: CommsBase {
        public IProcess RxProcess { get; private set; }
        protected IMyBroadcastListener _broadcast_recv;
        protected long _trustedSender = 0;

        public DroneCommsProcess(Logger log, IMyIntergridCommunicationSystem IGC, MyIni ini) : base(log, IGC, ini) {
            RxProcess = new MethodProcess(Run, Begin);
        }

        public void Begin() {   
            _IGC.UnicastListener.DisableMessageCallback();
            _broadcast_recv = _IGC.RegisterBroadcastListener("fzy.automine.discover");
        }

        protected IEnumerator<Void> Run() {
            while(_trustedSender == 0) {
                while(_broadcast_recv.HasPendingMessage) {
                    var msg = _broadcast_recv.AcceptMessage();
                    var authkeyString = msg.Data as string;
                    if(authkeyString == null || TryParseKey(authkeyString, ref PARSE_AUTHKEY)) { continue; }
                    Code(ref PARSE_AUTHKEY);

                    if(PARSE_AUTHKEY.Equals(AUTHKEY)) {
                        _trustedSender = msg.Source;
                        _log.Log($"{_trustedSender} has good key");
                        _IGC.DisableBroadcastListener(_broadcast_recv);
                        _IGC.UnicastListener.SetMessageCallback();
                    }
                }
                yield return Void._;
            }
            for(;;) {
                while(_IGC.UnicastListener.HasPendingMessage) {
                    var msg = _IGC.UnicastListener.AcceptMessage();
                    if(msg.Source != _trustedSender) { continue; }
                    if(msg.Tag == "ping") {
                        _IGC.SendUnicastMessage<object>(_trustedSender, "pong", null);
                        _log.Log($"pong -> {_trustedSender}");
                    }
                }
                yield return Void._;
            }
        }
    }

    class StationCommsProcess: CommsBase {
        public readonly IProcess RxProc; 
        public readonly IProcess TxProc;
        public StationCommsProcess(Logger log, IMyIntergridCommunicationSystem IGC, MyIni ini) : base(log, IGC, ini) {
            RxProc = new MethodProcess(RunRx, BeginRx);
        }

        public void BeginRx() {
            _IGC.UnicastListener.SetMessageCallback();
        }

        protected IEnumerator<Void> RunRx() {
            for(;;) {
                while(_IGC.UnicastListener.HasPendingMessage) {
                    var msg = _IGC.UnicastListener.AcceptMessage();
                }
            }
        }
    }
}
