using Sandbox.ModAPI.Ingame;
using System;
using VRage.Game.GUI.TextPanel;
using VRageMath;

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
