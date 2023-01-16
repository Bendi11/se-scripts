using Sandbox.ModAPI.Ingame;
using System;
using System.Text;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
    /// <summary>
    /// Simple logging facility supporting many screen sizes and automatic text wrapping
    /// </summary>
    public static class Log {
        static IMyTextSurface terminal;
        static int msg_count = 0;
        static Vector2 BOUNDS;

        public static void Init(IMyTextSurface term) {
            terminal.WriteText("", false);
            terminal = term;
            terminal.BackgroundColor = Color.Black;
            terminal.ContentType = ContentType.TEXT_AND_IMAGE;
            terminal.Font = "Monospace";
            terminal.FontColor = Color.Lime;
            terminal.FontSize = 0.7F;
            terminal.TextPadding = 0F;
            var sb = new StringBuilder();
            sb.Append('A');
            var sz = terminal.MeasureStringInPixels(sb, "Monospace", terminal.FontSize);
            BOUNDS = (terminal.SurfaceSize - sz) / sz;
        }
        
        /// <summary>
        /// Display a given panic message on the terminal and throw an exception with the message
        /// </summary>
        public static void Panic(string msg) {
            Log("[FTL]" + msg);
            throw new Exception(msg);
        }

        public static void Error(string msg) { Log("[ERR]" + msg); }
        public static void Warn(string msg) { Log("[WRN]" + msg); }

        public static void Log(string msg) {
            if(msg_count >= BOUNDS.Y) {
                terminal.WriteText("", false);
                msg_count = 0;
            }
            
            while(msg.Length > 0) {
                int len = Math.Min((int)BOUNDS.X, msg.Length);
                terminal.WriteText(msg.Substring(0, len), true);
                msg = msg.Substring(len);
                terminal.WriteText("\n", true);
                msg_count += 1;
            }
        }
    }
}
