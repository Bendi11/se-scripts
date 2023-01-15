using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;
using System.Collections.Immutable;

namespace IngameScript {
    /// <summary>
    /// General purpose unicast communications protocol with boilerplate
    /// reducing methods to register actions for specific requests
    /// </summary>
    class Sendy {
        public readonly IProcess RecvProcess, PeriodicProcess;
        public string Domain;

        public int TicksPerPeriod = 10;
        public int DiscoverPeriod = 1000;

        public const string 
            SENDY_DOMAIN = "sendy",
            ESTABLISH = SENDY_DOMAIN + ".establish",
            ESTABLISH_CONF = ESTABLISH + ".confirm",
            ESTABLISH_DENY = ESTABLISH + ".deny",
            DISCOVER = SENDY_DOMAIN + ".discover";

        IMyIntergridCommunicationSystem _igc;
        IMyBroadcastListener _discover;
        Dictionary<long, Connection> _connections;

        ImmutableDictionary<string, Action<MyIGCMessage>> _defaultActions;
        Dictionary<long, long> a;

        public Sendy(IMyIntergridCommunicationSystem IGC, string domain) {
            Domain = domain;
            _igc = IGC;
            RecvProcess = new MethodProcess(Receive, StartReceive);
            _defaultActions = new Dictionary<string, Action<MyIGCMessage>> {
                {
                    ESTABLISH,
                    (msg) => {
                        var d = msg.Data as string ?? "";
                        if(d.Equals(Domain)) {
                            _igc.SendUnicastMessage(msg.Source, ESTABLISH_CONF, 0);
                            ConfirmConnection(msg.Source);
                        } else {
                            _igc.SendUnicastMessage(msg.Source, ESTABLISH_DENY, "unknown domain"); 
                        }
                    }
                },
                {
                    ESTABLISH_CONF,
                    (msg) => {

                    }
                }
            }.ToImmutableDictionary();
        }

        private void ConfirmConnection(long source) {
            var conn = new Connection(source);
            _connections.Add(source, conn);
        }

        /// <summary>
        /// Attempt to send a unicast connection establish message to the given address
        /// </summary>
        public void Establish(long addr, string domain = null) => _igc.SendUnicastMessage(addr, ESTABLISH, domain ?? Domain);

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
                } else if(!value){
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
                if(TransmitBroadcast) {
                    _ticksSinceBroadcast += TicksPerPeriod;
                    if(_ticksSinceBroadcast >= DiscoverPeriod) {
                        _ticksSinceBroadcast = 0;
                        _igc.SendBroadcastMessage(DISCOVER, Domain);
                    }
                }
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
            var first = _defaultActions.GetValueOrDefault(msg.Tag);
            if(first != null) first(msg);
        }
    }
}
