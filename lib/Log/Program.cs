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
        static IMyTextSurface _term;
        static int _lines = 0;
        static Vector2 BOUNDS;

        public static void Init(IMyTextSurface term) {
            _term = term;
            _term.WriteText("", false);
            _term.BackgroundColor = Color.Black;
            _term.ContentType = ContentType.TEXT_AND_IMAGE;
            _term.Font = "Monospace";
            _term.FontColor = Color.Lime;
            _term.FontSize = 0.7F;
            _term.TextPadding = 0F;
            var sb = new StringBuilder();
            sb.Append('A');
            var sz = _term.MeasureStringInPixels(sb, "Monospace", _term.FontSize);
            BOUNDS = (_term.SurfaceSize - sz) / sz;
        }
        
        /// <summary>
        /// Display a given panic message on the terminal and throw an exception with the message
        /// </summary>
        public static void Panic(string msg) {
            Put("[FTL]" + msg);
            throw new Exception(msg);
        }

        public static void Error(string msg) => Put("[ERR]" + msg);
        public static void Warn(string msg) => Put("[WRN]" + msg);

        public static void Put(string msg) {
            if(_lines >= BOUNDS.Y) {
                _term.WriteText("", false);
                _lines = 0;
            }
            
            while(msg.Length > 0) {
                int len = Math.Min((int)BOUNDS.X, msg.Length);
                _term.WriteText(msg.Substring(0, len), true);
                msg = msg.Substring(len);
                _term.WriteText("\n", true);
                _lines += 1;
            }
        }
    }
}
