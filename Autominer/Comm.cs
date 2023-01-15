using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Collections.Immutable;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript {
    class CommsBase {
        protected Logger _log;
        protected readonly byte[] AUTHKEY = new byte[8];
        protected byte[] PARSE_AUTHKEY = new byte[8];

        public const string
            DOMAIN = "fzy.automine";

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

        public Sendy Station(IMyIntergridCommunicationSystem IGC) => new Sendy(_log, IGC, DOMAIN, new Dictionary<string, Sendy.IDispatch>()) {
            ListenForBroadcast = true
        };

        public Sendy Drone(IMyIntergridCommunicationSystem IGC) => new Sendy(_log, IGC, DOMAIN, new Dictionary<string, Sendy.IDispatch>()) {
            TransmitBroadcast = true
        };
    }
}
