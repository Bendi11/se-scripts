using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System;
using System.Collections.Immutable;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript {
    class CommsBase {
        protected Logger _log;
        protected IMyIntergridCommunicationSystem _IGC;
        protected readonly byte[] AUTHKEY = new byte[8];
        protected byte[] PARSE_AUTHKEY = new byte[8];
        protected const string BROADCAST_TAG = "fzy.automine.discover";
        protected const string PING_TAG = "ping";
        protected const string REGISTER_TAG = "logon"; 

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
            _broadcast_recv = _IGC.RegisterBroadcastListener(BROADCAST_TAG);
        }

        protected IEnumerator<Void> Run() {
            while(_trustedSender == 0) {
                while(_broadcast_recv.HasPendingMessage) {
                    var msg = _broadcast_recv.AcceptMessage();
                    if(msg.Data is ImmutableArray<byte>) {
                        var recvKey = (ImmutableArray<byte>)msg.Data;
                        if(recvKey.Length != PARSE_AUTHKEY.Length) { continue; }
                        for(int i = 0; i < recvKey.Length; ++i) { PARSE_AUTHKEY[i] = recvKey[i]; }
                        //recvKey.AsMemory().CopyTo(PARSE_AUTHKEY.AsMemory());
                        Code(ref PARSE_AUTHKEY);

                        if(PARSE_AUTHKEY.Equals(AUTHKEY)) {
                            _trustedSender = msg.Source;
                            _log.Log($"{_trustedSender} has good key");
                            _IGC.DisableBroadcastListener(_broadcast_recv);
                            _IGC.UnicastListener.SetMessageCallback();
                            _IGC.SendUnicastMessage<Int64>(_trustedSender, REGISTER_TAG, _IGC.Me);
                        }
                    }
                }
                yield return Void._;
            }
            for(;;) {
                while(_IGC.UnicastListener.HasPendingMessage) {
                    var msg = _IGC.UnicastListener.AcceptMessage();
                    if(msg.Source != _trustedSender) { continue; }
                    if(msg.Tag == PING_TAG) {
                        _IGC.SendUnicastMessage<object>(_trustedSender, PING_TAG, null);
                        _log.Log($"ping resp. -> {_trustedSender}");
                    }
                }
                yield return Void._;
            }
        }
    }

    class StationCommsProcess: CommsBase {
        public readonly IProcess RxProc; 
        public readonly IProcess TxProc;

        struct ClientData {
            public long Address;
            public int DroppedPings;

            public ClientData(long addr) {
                Address = addr;
                DroppedPings = 0;
            }
        }
        
        Dictionary<long, ClientData> _connected = new Dictionary<long, ClientData>();

        public StationCommsProcess(Logger log, IMyIntergridCommunicationSystem IGC, MyIni ini) : base(log, IGC, ini) {
            RxProc = new MethodProcess(RunRx, BeginRx);
        }
        

        void BeginRx() {
            _IGC.UnicastListener.SetMessageCallback();
        }

        IEnumerator<Void> RunRx() {
            for(;;) {
                while(_IGC.UnicastListener.HasPendingMessage) {
                    var msg = _IGC.UnicastListener.AcceptMessage();
                    switch(msg.Tag) {
                        case REGISTER_TAG:
                            _connected.Add(msg.Source, new ClientData(msg.Source));
                            _log.Log($"logon {msg.Source}");
                        break;

                        case PING_TAG:
                            if(_connected.ContainsKey(msg.Source)) {
                                var client = _connected[msg.Source];
                                client.DroppedPings = Math.Max(client.DroppedPings - 1, 0);
                            }
                        break;
                    } 
                }
            }
        }

        IEnumerator<Void> RunTx() {
            for(;;) {
                for(int i = 0; i < 10; ++i) yield return Void._;
                PARSE_AUTHKEY = AUTHKEY;
                Code(ref PARSE_AUTHKEY);
                _IGC.SendBroadcastMessage(BROADCAST_TAG, ImmutableArray.Create(PARSE_AUTHKEY));
                _log.Log("key broadcast");

                yield return Void._;
                
                long toRemove = -1;
                foreach(var addr in _connected.Keys) {
                    var client = _connected[addr];
                    if(client.DroppedPings > 5) {
                        _log.Warn($"end conn @ {addr} ({client.DroppedPings} dropped pings)");
                        toRemove = client.Address;
                        break;
                    }

                    client.DroppedPings += 1;
                    _IGC.SendUnicastMessage<object>(addr, PING_TAG, null);
                    _log.Log($"ping -> {addr}");
                }

                if(toRemove != -1) {
                    _connected.Remove(toRemove);
                }
            }
        }
    }
}
