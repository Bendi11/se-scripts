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
    public abstract class Host: Sendy<long> {
        public struct Device {
            long Addr;
            string Name;
            string Description;

            public static IEnumerable<Nullable<Device>> Connect(Host host, long addr) {
                var me = new Device();
                me.Addr = addr;

                var wait = host.WaitResponse(
                    host.SendRequest(addr, (long)SendyLink.Command.Name),
                    10
                );

                foreach(var p in wait) { yield return null; }
                var name = wait.GetEnumerator().Current;
                if(!name.HasValue || !(name.Value.Data is string)) {
                    Log.Warn($"rt {addr}: Failed to get name");
                    yield break;
                }

                me.Name = name.Value.Data as string;

                wait = host.WaitResponse(host.SendRequest(addr, (long)SendyLink.Command.Description));
                foreach(var p in wait) { yield return null; }
                var desc = wait.GetEnumerator().Current;
                if(!desc.HasValue || !(name.Value.Data is string)) {
                    Log.Warn($"rt {addr}: Failed to get description");
                }

                me.Description = desc.Value.Data as string;

                yield return me;
            }
        }

        /// A remotely-operated wide-fov targeting device
        public struct TGP {
            public Device Device;
            Host _host;

            public static IEnumerator<Nullable<TGP>> Connect(Host host, long addr) {
                TGP self = new TGP();
                self._host = host;
                
                var wait = Device.Connect(host, addr);
                foreach(var _ in wait) { yield return null; }
                var device = wait.GetEnumerator().Current;
                if(!device.HasValue) { yield break; }

                yield return self;
            }
        }

        public List<TGP> TGPs;

        public Host(IMyIntergridCommunicationSystem IGC) : base(IGC) {
            
        }

        public IEnumerator<object> DiscoverDevices(double timeOut = 15) {
            long broadcast = BroadcastRequest(
                SendyLink.DOMAIN,
                (long)SendyLink.Command.Name
            );

            var broadcastResponses = WaitResponses(broadcast, timeOut);
            foreach(var contact in broadcastResponses) {
                if(!contact.HasValue) {
                    yield return null;
                }

                IEnumerable<Nullable<Response>> wait;

                 
            }
        }
    }
}
