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
using SpaceEngineers.Game.Entities.Blocks;

struct Text: IDrawable {
    StringBuilder _text;
    public float Size;

    const string FONT = "White";

    public Text(string text, float size = 1f) { _text = new StringBuilder(text); Size = size; }
    
    /// Get the size in scaled units of the text when rendered
    public Vector2 GetRenderedSize(Renderer r) {
        return r
            ._root
            .MeasureStringInPixels(_text, FONT, r.ScaleFactor.Length() / 160 * Size) / r.ScaleFactor;
    }

    public void Draw(Renderer r) {
        r.Draw(new MySprite() {
            Type = SpriteType.TEXT,
            Alignment = TextAlignment.CENTER,
            Data = _text.ToString(),
            Position = Vector2.Zero,
            RotationOrScale = r.ScaleFactor.Length() / 160 * Size,
            Color = Color.Red,
            FontId = FONT,
        });
    }
}

class Root: IDrawable {
    
    public void Draw(Renderer r) {
        /*Slots slots = new Slots(cfg, null);
        slots.Roll();
        slots.Draw(r);
        /*var sz = r.Size.X / 14f;
        r.Scale(sz);
        r.Translate(-14f, -4f);
        bool red = true;
        for(CardKind kind = CardKind.Heart; kind < CardKind.COUNT; ++kind) {
            r.Translate(0f, 1f);
            for(CardNumeral num = CardNumeral.One; num <= CardNumeral.Ace; ++num) {
                r.Translate(1f, 0f);
                r.Draw(new Card(kind, num, red));
                red = !red;
                r.Translate(1f, 0f); 
            }
            
            red = !red;
            r.Translate(-28f, 1.5f);
        }*/
    }
}

namespace IngameScript {
    partial class Program: MyGridProgram {
        Slots slots;

        SlotGameConfig cfg = new SlotGameConfig(
            new SlotIcon[] {
                new SlotIcon() {
                    Sprite = @"Textures\FactionLogo\Builders\BuilderIcon_1.dds",
                    Probability = 0.2f,
                    Color = Color.Red,
                },
                new SlotIcon() {
                    Sprite = @"Textures\FactionLogo\Builders\BuilderIcon_13.dds",
                    Probability = 0.1f,
                    Color = Color.Yellow,
                },
                new SlotIcon() {
                    Sprite = @"Textures\FactionLogo\Builders\BuilderIcon_7.dds",
                    Probability = 0.05f,
                    Color = Color.Green,
                }
            },
            Color.Purple,
            3
        );

        public Program() {
            Log.Init(Me.GetSurface(0));
            Tasks.Init(Runtime);
            var disp = GridTerminalSystem.GetBlockWithName("[SLOT] CS0-0") as IMyTextSurfaceProvider;
            slots = new Slots(cfg, disp);
            Tasks.Spawn(slots.Roll());
            Runtime.UpdateFrequency |= UpdateFrequency.Once;
        }

        public void Save() {
            
        }

        public void Main(string argument, UpdateType updateSource) {
            if(argument == "roll") { Tasks.Spawn(slots.Roll()); }
            try {
            Tasks.RunMain();
            } catch(Exception e) {
                Log.Panic($"{e}");
            } 
        }
    }
}
