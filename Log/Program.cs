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
    public class Logger {
        IMyTextSurface terminal;
        int msg_count = 0;

        public Logger(IMyTextSurface terminal) {
            terminal.WriteText("", false);
            this.terminal = terminal;
            terminal.BackgroundColor = Color.Black;
            terminal.ContentType = ContentType.TEXT_AND_IMAGE;
            terminal.Font = "Monospace";
            terminal.FontColor = Color.Lime;
            terminal.FontSize = 1.3F;
        }

        public void Error(string msg) { Log("[ERR]" + msg); }
        public void Warn(string msg) { Log("[WRN]" + msg); }

        public void Log(string msg) {
            if(msg_count >= 10) {
                terminal.WriteText("", false);
                msg_count = 0;
            }
            
            while(msg.Length > 0) {
                int len = Math.Min(20, msg.Length);
                terminal.WriteText(msg.Substring(0, len), true);
                msg = msg.Substring(len);
                terminal.WriteText("\n", true);
            }

            msg_count += 1; 
        }
    }
}
