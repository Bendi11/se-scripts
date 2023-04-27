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
        public class Device {
            public long Addr;
            public SendyLink.RemoteTerminalData Data;
            public Host Host;

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

        public Dictionary<long, Device> Devices = new Dictionary<long, Device>();

        public enum SPIModeKind {
            LocalDir,
            LocalPos,
            WorldDir,
            WorldPos,
            Target
        }

        public SPIModeKind SPIMode = SPIModeKind.LocalDir;
        public SendyLink.TargetData SPI = new SendyLink.TargetData();
        public Device SelectedWeapon = null;

        public Host(IMyIntergridCommunicationSystem IGC) : base(IGC) {
            
        }

        public override void HandleRequest(Sendy<long>.Request req) {
            var cmd = (SendyLink.Command)req.Method;
            switch(cmd) {
                case SendyLink.Command.Connect: {
                    var data = SendyLink.RemoteTerminalData.Decode(req.Data);
                    if(!data.HasValue) { Log.Panic("SendyLink malformed data field"); }
                    var device = new Device() {
                        Addr = req.Address,
                        Data = data.Value,
                        Host = this,
                    };

                    Respond(req);

                    Devices.Add(req.Address, device);
                } break;
            }
        }
    }
}
