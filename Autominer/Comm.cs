using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript {
    class CommProcess: IProcess {
        Logger _log;
        IMyIntergridCommunicationSystem _IGC;
        IMyBroadcastListener _broadcast_recv;
        long _trustedSender = 0;
        readonly byte[] AUTHKEY = new byte[8];
        byte[] PARSE_AUTHKEY = new byte[8];

        public CommProcess(Logger log, IMyIntergridCommunicationSystem IGC, MyIni ini) {
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
        private bool TryParseKey(string key, ref byte[] output) {
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
        private void Code(ref byte[] output) {
            for(byte i = 0; i < output.Length; ++i) {
                output[i] ^= (byte)(i * 0x2F);
            }
        }

        protected override IEnumerator<Void> Run() {
            _IGC.UnicastListener.DisableMessageCallback();
            _broadcast_recv = _IGC.RegisterBroadcastListener("fzy.automine.discover");
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
                yield return VOID;
            }
            for(;;) {
                while(_IGC.UnicastListener.HasPendingMessage) {
                    var msg = _IGC.UnicastListener.AcceptMessage();
                    if(msg.Source != _trustedSender) { continue; }
                }
            }
        }
    }
}
