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
        public class ConnectedDevice {
            public long Address;
            public SendyLink.Capabilities Capabilities;
            public SendyLink.AcquisitionCapabilities AcquisitionCapabilities;
        }

        public List<ConnectedDevice> Devices = new List<ConnectedDevice>();

        public Host(IMyIntergridCommunicationSystem IGC) : base(IGC) {
            
        }

        public IEnumerator<object> DiscoverDevices() {
            long broadcast = BroadcastRequest(
                SendyLink.DOMAIN,
                SendyLink.Request.Name
            );

            var broadcastResponses = WaitResponses(broadcast, 15);
            foreach(var contact in broadcastResponses) {
                if(!contact.HasValue) {
                    yield return null;
                }

                IEnumerable<Nullable<Response>> wait;

                wait = WaitResponse(SendRequest(contact.Value.Address, SendyLink.Request.Capabilities));
                foreach(var prog in wait) { yield return null; }
                string description = wait.GetEnumerator().Current.Value.Data as string;
                
                wait = WaitResponse(SendRequest(contact.Value.Address, SendyLink.Request.AcquisitionCapabilities));
                foreach(var prog in wait) { yield return null; }

                var capabilities = (SendyLink.Capabilities)wait.GetEnumerator().Current.Value.Data;
                
                var acq = SendyLink.AcquisitionCapabilities.None;
                if(capabilities.HasFlag(SendyLink.Capabilities.Acquisition)) {
                    wait = WaitResponse(SendRequest(contact.Value.Address))
                }
            }
        }

        private class OnNameResponse: Process<MyTuple<long, object>, bool> {
            protected override IEnumerator<bool> Run(MyTuple<long, object> args) {
                ;
            }
        }
    }
}
