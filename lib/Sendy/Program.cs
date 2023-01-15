using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System;
using System.Collections.Immutable;

namespace IngameScript {
    public class Dispatch<T>: Sendy.IDispatch {
        Action<Sendy.Connection, T> _f;
        public Dispatch(Action<Sendy.Connection, T> f) {
            _f = f;
        }
        public bool Validate(object d) => d is T; 
        public void ExecuteRaw(Sendy.Connection c, object d) => _f(c, (T)d);
    }

    /// <summary>
    /// General purpose unicast communications protocol with boilerplate
    /// reducing methods to register actions for specific requests
    /// </summary>
    public class Sendy {
        public readonly IProcess RecvProcess, PeriodicProcess;
        public string Domain;

        public int TicksPerPeriod = 10;
        public int PingPeriod = 750;
        public int DiscoverPeriod = 500;
        public int PendingTimeout = 500;
        public int PingsBeforeDrop = 5;
        public Action<Connection> OnConnection;

        public const string 
            SENDY_DOMAIN = "sendy",
            CONN = SENDY_DOMAIN + ".conn",
            ESTABLISH_CONF = CONN + ".confirm",
            ESTABLISH_DENY = CONN + ".deny",
            DROP = CONN + ".drop",
            PING = SENDY_DOMAIN + ".ping",
            DISCOVER = SENDY_DOMAIN + ".discover";

        IMyIntergridCommunicationSystem _igc;
        IMyBroadcastListener _discover;
        Dictionary<long, Connection> _connections = new Dictionary<long, Connection>();

        ImmutableDictionary<string, Action<MyIGCMessage>> _defaultActions;
        ImmutableDictionary<string, IDispatch> _actions;
        PendingConnection _pending;
        Logger _log;

        struct PendingConnection {
            public long TicksPending, Node;
        }
        
        public Sendy(Logger log, IMyIntergridCommunicationSystem IGC, string domain, IDictionary<string, IDispatch> dict) : this(log, IGC, domain, dict.ToImmutableDictionary()) {}

        public Sendy(Logger log, IMyIntergridCommunicationSystem IGC, string domain, ImmutableDictionary<string, IDispatch> dict) {
            _log = log;
            Domain = domain;
            _igc = IGC;
            _pending.Node = -1;
            RecvProcess = new MethodProcess(Receive, StartReceive);
            PeriodicProcess = new MethodProcess(Periodic);
            _defaultActions = new Dictionary<string, Action<MyIGCMessage>> {
                {
                    CONN,
                    (msg) => {
                        _igc.SendUnicastMessage(msg.Source, ESTABLISH_CONF, 0);
                        ConfirmConnection(msg.Source);
                    }
                },
                {
                    ESTABLISH_CONF,
                    (msg) => {
                        if(_pending.Node == msg.Source) {
                            _pending.Node = -1;
                            ConfirmConnection(msg.Source);
                        }
                    }
                },
                {
                    DISCOVER,
                    (msg) => {
                        if(_connections.ContainsKey(msg.Source)) return;

                        if(msg.Data.Equals(Domain)) {
                            BeginEstablish(msg.Source, false); 
                        } else {
                            _log.Log($"{msg.Data.ToString()} != {Domain}");
                        }
                    }
                }
            }.ToImmutableDictionary();
            _actions = dict
                .Add(
                    PING,
                    new Dispatch<int>((conn, data) => conn.MissedPings = 0)
                )
                .Add(
                    DROP,
                    new Dispatch<int>((conn, data) => {
                        _connections.Remove(conn.Node);
                    })
                );
        }

        private void ConfirmConnection(long source) {
            var conn = new Connection(this, source);
            _connections.Add(source, conn);
            if(OnConnection != null) OnConnection(conn);
        }

        private void BeginEstablish(long addr, bool force) {
            if(_pending.Node != -1 && !force) { _log.Log("pending conn. blocks attempted connection"); return; }
            _pending.Node = addr;
            _pending.TicksPending = 0;
            _igc.SendUnicastMessage(addr, CONN, 0);
        }

        /// <summary>
        /// Attempt to send a unicast connection establish message to the given address
        /// </summary>
        public void Establish(long addr, bool force = false) => BeginEstablish(addr, force);

        /// <summary>
        /// If <c>true</c>, listen for broadcast messages from other nodes
        /// </summary>
        public bool ListenForBroadcast {
            get {
                return _discover != null; 
            }

            set {
                if(_discover == null && value) {
                    _discover = _igc.RegisterBroadcastListener(DISCOVER);
                    _discover.SetMessageCallback();
                } else if(!value){
                    _discover.DisableMessageCallback();
                    while(_discover.HasPendingMessage) _discover.AcceptMessage();
                    _igc.DisableBroadcastListener(_discover);
                }
            }
        }
        
        /// <summary>
        /// If <c>true</c>, transmit a broadcast signal requesting any nearby nodes establish a 
        /// connection
        /// </summary>
        public bool TransmitBroadcast = false;

        int _ticksSinceBroadcast = 0;

        private IEnumerator<Nil> Periodic() {
            for(;;) {
                var passedTicks = TicksPerPeriod;
                if(TransmitBroadcast) {
                    _ticksSinceBroadcast += passedTicks;
                    if(_ticksSinceBroadcast >= DiscoverPeriod) {
                        _ticksSinceBroadcast = 0;
                        _igc.SendBroadcastMessage(DISCOVER, Domain);
                    }
                }

                if(_pending.Node != -1) {
                    _pending.TicksPending += TicksPerPeriod;
                    if(_pending.TicksPending >= PendingTimeout) {
                        _log.Log($"pending {_pending.Node} timeout");
                        _pending.Node = -1;
                        break;
                    }
                }

                long toRemove = -1;
                foreach(var conn in _connections.Values) {
                    conn.TicksSincePing += passedTicks;
                    if(conn.TicksSincePing >= PingPeriod) {
                        conn.TicksSincePing = 0;
                        conn.MissedPings += 1;
                        conn.Send(PING, 0);
                        if(conn.MissedPings > PendingTimeout) {
                            toRemove = conn.Node;
                            _log.Log($"conn {conn.Node} ping timeout");
                            break;
                        }
                    }
                }

                if(toRemove != -1) _connections[toRemove].Close();

                yield return Nil._;
            }
        }

        private void StartReceive() {
            _igc.UnicastListener.SetMessageCallback();
        }

        private IEnumerator<Nil> Receive() {
            for(;;) {
                while(_igc.UnicastListener.HasPendingMessage)
                    ProcessMessage(_igc.UnicastListener.AcceptMessage()); 
                while(ListenForBroadcast && _discover.HasPendingMessage)
                    ProcessMessage(_discover.AcceptMessage());
                yield return Nil._;
            }
        }

        private void ProcessMessage(MyIGCMessage msg) {
            _log.Log($"{msg.Source} -> {msg.Tag} ({msg.Data.ToString()})");
            var first = _defaultActions.GetValueOrDefault(msg.Tag);
            if(first != null) { first(msg); return; }

            Connection conn;
            if(_connections.TryGetValue(msg.Source, out conn)) {
                var act = _actions.GetValueOrDefault(msg.Tag);
                if(act != null && act.Validate(msg.Data)) {
                    act.ExecuteRaw(conn, msg.Data);
                }
            } 
        }

        /// <summary>
        /// A single connection with another programmable block, keeping track of 
        /// connection status with pings and maintining a dictionary of tags to the actions
        /// that should be dispatched
        /// </summary>
        public class Connection {
            public readonly long Node;
            public long MissedPings, TicksSincePing;
            public readonly Sendy Sendy;
            public Action<Connection> OnDrop;
            
            public void Send<T>(string tag, T data) => Sendy._igc.SendUnicastMessage(Node, tag, data);
            public void Close() {
                if(OnDrop != null) OnDrop(this);
                Send<int>(Sendy.DROP, 0);
                Sendy._connections.Remove(Node);
            }

            public Connection(Sendy sendy, long addr) {
                Node = addr;
                Sendy = sendy;
                MissedPings = 0;
            }
        }

        public interface IDispatch {
            bool Validate(object data);
            void ExecuteRaw(Connection c, object data);
        }
    }
}
