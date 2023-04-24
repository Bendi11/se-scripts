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
            public long Addr;
            public string Name;
            public string Description;
            public Host Host;

            public static IEnumerable<Nullable<Device>> Connect(Host host, long addr) {
                var me = new Device();
                me.Host = host;
                me.Addr = addr;

                var wait = host.WaitResponse(
                    host.SendRequest(addr, (long)SendyLink.Command.Name),
                    10
                );

                foreach(var p in wait) { yield return null; }
                var name = Process.Get(wait);
                if(!name.HasValue || !(name.Value.Data is string)) {
                    Log.Warn($"rt {addr}: Failed to get name");
                    yield break;
                }

                me.Name = name.Value.Data as string;

                wait = host.WaitResponse(host.SendRequest(addr, (long)SendyLink.Command.Description));
                foreach(var p in wait) { yield return null; }
                var desc = Process.Get(wait);
                if(!desc.HasValue || !(desc.Value.Data is string)) {
                    Log.Warn($"rt {addr}: Failed to get description");
                    yield break;
                }

                me.Description = desc.Value.Data as string;

                yield return me;
            }

            public IEnumerable<Nullable<Res>> Cmd<Arg,Res>(long method, Arg arg) {
                var wait = Host.WaitResponse(Host.SendRequest(
                    Addr,
                    method,
                    arg
                ));

                foreach(var _ in wait) { yield return null; }
                var resp = Process.Get(wait);
                if(
                    resp.HasValue &&
                    resp.Value.Data != null &&
                    resp.Value.Data is Res
                ) {
                    yield return (Res)resp.Value.Data;
                }
            }
        }

        /// A remotely-operated wide-fov targeting device
        public struct TGP {
            public Device Device;

            public IEnumerable<Nullable<SendyLink.TargetData>> PointLock(double distance = 500) =>
                Device.Cmd<Double, SendyLink.TargetData?>((long)SendyLink.TGP.Command.PointLock, distance);

            public static IEnumerable<Nullable<TGP>> Connect(Host host, Device device) {
                TGP self = new TGP();
                self.Device = device;

                yield return self;
            }
        }

        public List<TGP> TGPs;

        public Host(IMyIntergridCommunicationSystem IGC) : base(IGC) {
            
        }

        public IEnumerator DiscoverDevices(double timeOut = 15) {
            long broadcast = BroadcastRequest(
                SendyLink.DOMAIN,
                (long)SendyLink.Command.Discover
            );

            var broadcastResponses = WaitResponses(broadcast, timeOut);
            foreach(var contact in broadcastResponses) {
                if(!contact.HasValue) {
                    yield return null;
                }
                
                Process.Spawn(ConnectTo(contact.Value.Address));
            }
        }

        IEnumerable ConnectTo(long addr) {
            var wait = Device.Connect(this, addr);
            foreach(var _ in wait) { yield return null; }
            var device = Process.Get(wait);
            if(!device.HasValue) { yield break; }
        }
    }
}
