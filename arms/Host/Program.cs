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
    public class Host: Sendy<long> {
        public struct Device {
            public long Addr;
            public string Name;
            public string Description;
            public SendyLink.Device Kind;
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
                if(!name.HasValue || !(name.Value.Data is string))
                    throw new Exception($"rt {addr}: Failed to get name");

                me.Name = name.Value.Data as string;

                wait = host.WaitResponse(
                    host.SendRequest(addr, (long)SendyLink.Command.Description),
                    10
                );
                foreach(var p in wait) { yield return null; }
                var desc = Process.Get(wait);
                if(!desc.HasValue || !(desc.Value.Data is string)) 
                    throw new Exception($"rt {addr}: Failed to get description");

                me.Description = desc.Value.Data as string;

                var kindProc = host.WaitResponse(
                    host.SendRequest(addr, (long)SendyLink.Command.Kind),
                    10
                )
                    .Select(v => v.HasValue ? v.Value.Data as long? : null);
                foreach(var _ in kindProc) { yield return null; }
                var kind = Process.Get(kindProc);
                if(!kind.HasValue) 
                    throw new Exception($"rt {addr}: Failed to get device kind");

                me.Kind = (SendyLink.Device)kind;

                yield return me;
            }

            public IEnumerable<Nullable<Res>> Cmd<Arg,Res>(long method, Arg arg) where Res: struct {
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
        public struct RemoteTGP {
            public Device Device;

            public IEnumerable<SendyLink.TargetData?> PointLock(double distance = 500) =>
                Device
                    .Cmd<Double, long>((long)SendyLink.TGP.Command.PointLock, distance)
                    .Select(v => v.HasValue ? SendyLink.TargetData.Decode(v.Value) : null);

            public IEnumerable<SendyLink.SPIMode?> GetSPIMode() =>
                Device
                    .Cmd<object, long>((long)SendyLink.TGP.Command.GetSPIMode, null)
                    .Select(v => (SendyLink.SPIMode?)v);

            public void SetSPIMode(SendyLink.SPIMode mode) =>
                Device.Host.SendRequest(Device.Addr, (long)SendyLink.TGP.Command.SetSPIMode, (long)mode);

            public void SetSPI(SendyLink.TargetData spi) =>
                Device.Host.SendRequest(Device.Addr, (long)SendyLink.TGP.Command.SetSPI, spi.Encode());

            public IEnumerable<SendyLink.TargetData?> GetSPI() {
                var wait = Device.Host.WaitResponse(
                    Device.Host.SendRequest(Device.Addr, (long)SendyLink.TGP.Command.GetSPI)
                );
                foreach(var _ in wait) { yield return null; }
                var resp = Process.Get(wait);
                if(resp.HasValue && resp.Value.Data != null) {
                    yield return SendyLink.TargetData.Decode(resp.Value.Data);
                }
            }

            public static RemoteTGP Connect(Device device) => new RemoteTGP() {
                Device = device,
            };
        }

        public Dictionary<long, Device> Devices = new Dictionary<long, Device>();

        public Host(IMyIntergridCommunicationSystem IGC) : base(IGC) {
            
        }

        public override void HandleRequest(Sendy<long>.Request req) {

        }

        public IEnumerable<Nil> DiscoverDevices(double timeOut = 15) {
            long broadcast = BroadcastRequest(
                SendyLink.DOMAIN,
                (long)SendyLink.Command.Discover
            );

            var broadcastResponses = WaitResponses(broadcast, timeOut);
            foreach(var contact in broadcastResponses) {
                if(!contact.HasValue) {
                    yield return Nil._;
                }
                
                Process.Spawn(
                    ConnectTo(contact.Value.Address)
                );
            }
        }

        IEnumerable<Nil> ConnectTo(long addr) {
            var wait = Device.Connect(this, addr);
            foreach(var _ in wait) { yield return Nil._; }
            var device = Process.Get(wait);
            if(!device.HasValue) { yield break; }
            
            Devices[addr] = device.Value;
        }
    }
}
