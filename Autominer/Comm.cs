using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System;
using System.Linq;
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

    class DroneCommsProcess: CommsBase {
        public IProcess RxProcess { get; private set; }
        public IProcess TxProcess { get; private set; }
        IMyBroadcastListener _broadcast_recv;
        long _trustedSender = -1;
        ulong _ticksWithoutPing = 0;

        public DroneCommsProcess(Logger log, IMyIntergridCommunicationSystem IGC, MyIni ini) : base(log, IGC, ini) {
            RxProcess = new MethodProcess(Run, Begin);
            TxProcess = new MethodProcess(PingCounter);
        }

        void Begin() {
            _IGC.UnicastListener.DisableMessageCallback();
            _broadcast_recv = _IGC.RegisterBroadcastListener(BROADCAST_TAG);
            _broadcast_recv.SetMessageCallback();
        }

        IEnumerator<Nil> PingCounter() {
            for(;;) {
                while(_trustedSender == -1) yield return Nil._;
                _ticksWithoutPing += 100;
                if(_ticksWithoutPing > 1000) {
                    _log.Error($"ping timeout for ${_trustedSender} conn. drop");
                    _trustedSender = -1;
                    _ticksWithoutPing = 0;
                    Begin();
                }
                yield return Nil._;
            }
        }

        IEnumerator<Nil> Run() {
            while(_trustedSender == -1) {
                while(_broadcast_recv.HasPendingMessage) {
                    var msg = _broadcast_recv.AcceptMessage();
                    if(msg.Data is ImmutableArray<byte>) {
                        var recvKey = (ImmutableArray<byte>)msg.Data;
                        if(recvKey.Length != PARSE_AUTHKEY.Length) { _log.Log("rec key len is not equal"); continue; }
                        recvKey.CopyTo(0, PARSE_AUTHKEY, 0, PARSE_AUTHKEY.Length);
                        Code(PARSE_AUTHKEY);

                        if(Enumerable.SequenceEqual(PARSE_AUTHKEY, AUTHKEY)) {
                            _trustedSender = msg.Source;
                            _log.Log($"{_trustedSender} has good key");
                            _IGC.DisableBroadcastListener(_broadcast_recv);
                            _IGC.UnicastListener.SetMessageCallback();
                            _IGC.SendUnicastMessage<Int64>(_trustedSender, REGISTER_TAG, _IGC.Me);
                        }
                    }
                }

                yield return Nil._;
            }

            for(;;) {
                while(_IGC.UnicastListener.HasPendingMessage) {
                    var msg = _IGC.UnicastListener.AcceptMessage();
                    if(msg.Source != _trustedSender) { continue; }
                    if(msg.Tag == PING_TAG) {
                        _IGC.SendUnicastMessage<Boolean>(_trustedSender, PING_TAG, true);
                        _log.Log($"ping resp. -> {_trustedSender}");
                    }
                }
                yield return Nil._;
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
            TxProc = new MethodProcess(RunTx);
        }
        

        void BeginRx() {
            _IGC.UnicastListener.SetMessageCallback();
        }

        IEnumerator<Nil> RunRx() {
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
                yield return Nil._;
            }
        }

        IEnumerator<Nil> RunTx() {
            for(;;) {
                for(int i = 0; i < 5; ++i) { yield return Nil._; }
                AUTHKEY.CopyTo(PARSE_AUTHKEY, 0);
                Code(PARSE_AUTHKEY);
                _IGC.SendBroadcastMessage(BROADCAST_TAG, ImmutableArray.Create(PARSE_AUTHKEY));

                yield return Nil._;
                
                long toRemove = -1;
                foreach(var addr in _connected.Keys) {
                    var client = _connected[addr];
                    if(client.DroppedPings > 5) {
                        _log.Warn($"end conn @ {addr} ({client.DroppedPings} dropped pings)");
                        toRemove = client.Address;
                        break;
                    }

                    client.DroppedPings += 1;
                    _IGC.SendUnicastMessage<Boolean>(addr, PING_TAG, false);
                    _log.Log($"ping -> {addr}");
                }

                if(toRemove != -1) {
                    _connected.Remove(toRemove);
                }
            }
        }
    }
}
