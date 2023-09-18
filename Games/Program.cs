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

struct Text: IDrawable {
    StringBuilder _text;

    const string FONT = "White";

    public Text(string text) { _text = new StringBuilder(text); }
    
    /// Get the size in scaled units of the text when rendered
    public Vector2 Size(Renderer r) {
        return r._root.MeasureStringInPixels(_text, FONT, r.ScaleFactor.Length() / 160) / r.ScaleFactor;
    }

    public void Draw(Renderer r) {
        r.Draw(new MySprite() {
            Type = SpriteType.TEXT,
            Alignment = TextAlignment.CENTER,
            Data = _text.ToString(),
            Position = Vector2.Zero,
            RotationOrScale = r.ScaleFactor.Length() / 160,
            Color = Color.Red,
            FontId = FONT,
        });
    }
}

namespace IngameScript {
    partial class Program: MyGridProgram {
        Display display;

        public Program() {
            var disp = GridTerminalSystem.GetBlockWithName("DISPLAY") as IMyTextSurfaceProvider;
            display = new Display(disp.GetSurface(0), new Card(CardKind.Clover, CardNumeral.Seven));
            Log.Init(Me.GetSurface(0));
            display.Update();
            Runtime.UpdateFrequency |= UpdateFrequency.Update10;
        }

        public void Save() {
            
        }

        public void Main(string argument, UpdateType updateSource) {
            Process.RunMain(Runtime.TimeSinceLastRun.TotalSeconds);
        }

    }
}


