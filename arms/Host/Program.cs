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
    public abstract class Host: Sendy<SendyLink.Request> {
        public readonly Process<Nil> Process;

        public class ConnectedDevice {
            public long Address;
            public SendyLink.Capabilities Capabilities;
            public SendyLink.AcquisitionCapabilities AcquisitionCapabilities;
        }

        public List<ConnectedDevice> Devices = new List<ConnectedDevice>();

        public Host(IMyIntergridCommunicationSystem IGC) : base(IGC) {
            Process = new MethodProcess(Run);
        }

        public IEnumerator<Nil> DiscoverDevices() {
            long broadcast = BroadcastRequest(
                SendyLink.DOMAIN,
                SendyLink.Request.Name,
                0
            );

            Process<object> wait = AwaitResponses(
                broadcast,
                (addr, nameObj) => {
                    var device = new ConnectedDevice() {
                        Address = addr
                    };

                    OnResponse(
                        SendRequest(addr, SendyLink.Request.Capabilities),
                        (_, resp) => {
                            device.Capabilities = (SendyLink.Capabilities)(long)resp;
                            if(device.Capabilities.HasFlag(SendyLink.Capabilities.Acquisition)) {
                                OnResponse(
                                    SendRequest(addr, SendyLink.Request.AcquisitionCapabilities),
                                    (__, r) => {
                                        device.AcquisitionCapabilities = (SendyLink.AcquisitionCapabilities)(long)r;
                                    }
                                );
                            }
                        }
                    );

                    return false;
                }
            );
        }

        protected abstract IEnumerator<Nil> Run();
    }
}
