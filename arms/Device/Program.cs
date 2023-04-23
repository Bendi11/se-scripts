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
    public abstract class Device: Sendy<SendyLink.Request> {
        public readonly Process<Nil> Process;
        public string Name, Description;

        public SendyLink.Capabilities Capabilities;
        public SendyLink.AcquisitionCapabilities AcquisitionCapabilities;
        public SendyLink.TargetData SPI = new SendyLink.TargetData() {
            Vec = Vector3D.Forward,
        };
        public SendyLink.SPIMode SPIMode = SendyLink.SPIMode.LocalDir;
        public bool Enabled = false;

        public Device(IMyIntergridCommunicationSystem IGS, string name, string description) : base(IGS) {
            IGC.UnicastListener.SetMessageCallback();
            Broadcast = IGC.RegisterBroadcastListener(SendyLink.DOMAIN);
            Broadcast.SetMessageCallback();

            Name = name;
            Description = description;
            Process = new MethodProcess(Run);
        

        protected abstract IEnumerator<Nil> Run();

        public override void HandleRequest(Sendy<SendyLink.Request>.Request req) {
            switch(req.Method) {
                case SendyLink.Request.Name: Respond(req, Name); break;
                case SendyLink.Request.Description: Respond(req, Description); break;
                case SendyLink.Request.Capabilities: Respond(req, (long)Capabilities); break;
                case SendyLink.Request.AcquisitionCapabilities: Respond(req, (long)AcquisitionCapabilities); break;
                case SendyLink.Request.Enable:
                    Enabled = true;
                break;
                case SendyLink.Request.Disable: Enabled = false; break;
                case SendyLink.Request.SPIModeSet: SPIMode = (SendyLink.SPIMode)(long)req.Data; break;
                case SendyLink.Request.SPISet: {
                    var newSPI = SendyLink.TargetData.Decode(req.Data);
                    SPI = newSPI.HasValue ? newSPI.Value : SPI;
                } break;
            } 
        }
    }
}
