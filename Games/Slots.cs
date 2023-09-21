
using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRage.Game.GUI.TextPanel;
using VRageMath;

/// All information for an icon as displayed on the slot machine,
/// with data required for randomly selecting the icon on the virtual reel
public struct SlotIcon: IDrawable {
    public string Sprite;
    public float Probability;
    public Color Color;

    public void Draw(Renderer r) {
        r.Draw(new MySprite() {
            Type = SpriteType.TEXTURE,
            Position = Vector2.Zero,
            Size = Vector2.One * 2f,
            Color = this.Color,
            Data = Sprite,
        });
    }
}

/// Shared state between multiple `Slots` instances, containing theme and probability information
public class SlotGameConfig {
    public SlotIcon[] Slots;
    public Color Background;
    public int ReelsCount;
    float _probabilitySum;
    public float ProbabilitySum { get { return _probabilitySum; } }

    static Random _rand = new Random();

    public SlotGameConfig(SlotIcon[] slots, Color background, int reels) {
        Slots = slots;
        Background = background;
        ReelsCount = reels;
        
        _probabilitySum = 0f;
        foreach(var slot in Slots) {
            _probabilitySum += slot.Probability;
        }
    }

    public int RandomIcon() {
        float draw = (float)_rand.NextDouble() * _probabilitySum;
        int drawnIndex = 0;
        foreach(var icon in Slots) {
            draw -= icon.Probability;
            if(draw <= 0f)
                break;

            drawnIndex += 1;
        }

        return drawnIndex;
    }
}

public struct Slots: IDrawable {
    /// Shared slot icon state, read from block custom data
    public SlotGameConfig Config;
    /// Storage for the generated slot icons, used to render onscreen 
    SelectedIcon[] _selectedIcons;
    IMyTextSurface _surface;

    struct SelectedIcon {
        public int Index;
        public bool Final;
    }
    
    /// Create a new slots instance from the map it will use to render icons to the user
    public Slots(SlotGameConfig cfg, IMyTextSurfaceProvider seat) {
        Config = cfg;
        _selectedIcons = new SelectedIcon[cfg.ReelsCount];
        _surface = seat.GetSurface(0);
    }
    
    /// Render a roll animation and select random icons for each reel
    public IEnumerator<Yield> Roll() {
        for(int i = 0; i < _selectedIcons.Length; ++i) {
            for(int k = 0; k < 20; ++k) {
                for(int j = i; j < _selectedIcons.Length; ++j) {
                    _selectedIcons[j] = new SelectedIcon() {
                        Index = Config.RandomIcon(),
                        Final = false,
                    };
                }
                
                var render = new Renderer(_surface);
                render.Draw(this);
                render.Dispose();
                yield return Tasks.WaitMs(50);
            }
            _selectedIcons[i].Final = true;
        }
        var render1 = new Renderer(_surface);
        render1.Draw(this);
        render1.Dispose();
    }
    
    /// Generate a random icon for the 
    public void Draw(Renderer r) {
        r._root.ScriptBackgroundColor = Config.Background;
        r.Translate(-r.Size.X / 2f, 0f);
        float xStep = r.Size.X / (float)Config.ReelsCount;
        float iconSize = xStep / 4f;
        float x = xStep / 2f;
        int reel = 0;
        foreach(var selected in _selectedIcons) {
            var icon = r.Push();
            icon.Translate(x, 0f);
            icon.Scale(iconSize);
            var color = Config.Slots[selected.Index].Color;
            icon.SetColor(selected.Final ? color : color * 0.5f);
            icon.Draw(Config.Slots[selected.Index]);
            reel += 1;
            x += xStep;
        } 
    }
}
