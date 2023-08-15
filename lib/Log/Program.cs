using Sandbox.ModAPI.Ingame;
using System;
using System.Text;
using VRage.Game.GUI.TextPanel;
using VRageMath;

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

/// <summary>
/// Write text onto LCD panels with automatic screen wrapping and font sizing
/// </summary>
public class LcdWriter {
    public IMyTextSurface LCD;
    public bool Wrap = true;
    readonly Vector2 _bounds, _defaultCharSize;
    readonly StringBuilder _sb = new StringBuilder();
    const string FONT = "Monospace";
    int _lines = 0;
    
    /// <summary>
    /// How many lines the LCD should be able to fit, adjusts font size
    /// </summary>
    public float FitLines {
        set { LCD.FontSize = FitLines / value; }

        get { return _bounds.Y / _defaultCharSize.Y; }
    }

    public float FitCols {
        set { LCD.FontSize = FitCols / value; }

        get { return _bounds.X / _defaultCharSize.X; }
    }

    public void Fit(float lines, float cols) {
        var fLines = FitLines / lines;
        var fCols = FitCols / cols;
        LCD.FontSize = Math.Min(fLines, fCols);
    }

    public LcdWriter(IMyTextSurface lcd) {
        _sb.Append('A');
        LCD = lcd;
        LCD.Font = FONT;
        LCD.FontSize = 1;
        LCD.TextPadding = 0F;
        _defaultCharSize = LCD.MeasureStringInPixels(_sb, FONT, 1);
    }
    
    public void Write(string msg) {
        if(_lines >= _bounds.Y) {
            LCD.WriteText("", false);
            _lines = 0;
        }
        
        do {
            int len = Math.Min((int)_bounds.X, msg.Length);
            LCD.WriteText(msg.Substring(0, len), true);
            msg = msg.Substring(len);
            LCD.WriteText("\n", true);
            _lines += 1;
        } while(Wrap && (msg.Length >= 0));
    }
}

