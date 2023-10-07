using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.GUI.TextPanel;
using VRageMath;

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
        //Slots slots;
        InputPad pad;

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

        IMyTextSurfaceProvider disp;

        IEnumerator<Yield> MainTask() {
            var render = new Renderer(disp.GetSurface(0));
            var menu = new Menu(disp as IMyShipController, new string[] { "Create Account", "Log In" });
            yield return Tasks.Async(menu.Select(render));
            var selected = Tasks.Receive<int>();
            if(selected == 1) {
                yield return Tasks.Async(pad.Input(render));
                var txt = Tasks.Receive<string>();
                Log.Error($"Entered: '{txt}' - {txt.GetHashCode()}");
            }
        }

        public Program() {
            Log.Init(Me.GetSurface(0));
            Tasks.Init(Runtime);
            disp = GridTerminalSystem.GetBlockWithName("[SLOT] CS0-0") as IMyTextSurfaceProvider;
            pad = new InputPad(disp as IMyShipController, 64, false, Color.Green);
            Tasks.Spawn(MainTask());
            Runtime.UpdateFrequency |= UpdateFrequency.Once;
        }

        public void Save() {
            
        }

        public void Main(string argument, UpdateType updateSource) {
            if(updateSource != UpdateType.Once) {
                if(argument.StartsWith("SENS-")) {
                    var args = argument.Substring(0, 5).Split('-');
                    if(args.Length == 2) {
                    
                    }
                }
            } else {
                Tasks.RunMain();
            }
        }
    }
}
