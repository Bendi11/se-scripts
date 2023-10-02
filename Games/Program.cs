using Sandbox.ModAPI.Ingame;
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
        NumPad pad;

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
            //slots = new Slots(cfg, disp);
            pad = new NumPad(disp as IMyShipController, 6, true, Color.White);
            //Tasks.Spawn(slots.Roll());
            Tasks.Spawn(pad.Input(new Renderer(disp.GetSurface(0)).Colored(Color.Red)));
            Runtime.UpdateFrequency |= UpdateFrequency.Once;
        }

        public void Save() {
            
        }

        public void Main(string argument, UpdateType updateSource) {
            if(updateSource != UpdateType.Once) {
                //Tasks.Spawn(slots.Roll());
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
