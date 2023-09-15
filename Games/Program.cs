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
    string _text;

    public Text(string text) { _text = text; }

    public Vector2 Size(Renderer r) {

    }

    public void Draw(Renderer r) {
        r.Draw(new MySprite() {
            Type = SpriteType.TEXT,
            Alignment = TextAlignment.CENTER,
            Data = _text,
            Position = Vector2.Zero,
            RotationOrScale = 0.1f,
            Color = Color.Red,
            FontId = "White",
        });
    }
}

namespace IngameScript {
    partial class Program: MyGridProgram {
        Display display;

        public Program() {
            var disp = GridTerminalSystem.GetBlockWithName("DISPLAY") as IMyTextSurfaceProvider;
            display = new Display(disp.GetSurface(0), new Card(CardKind.Heart, CardNumeral.Two));
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
