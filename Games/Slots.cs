
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
    /// Renderer used to draw the slots onscreen
    Renderer _render;
    /// Mutex used to stop rolls from overlapping
    static bool _mutex;

    struct SelectedIcon {
        public int Index;
        public bool Final;
    }
    
    /// Create a new slots instance from the map it will use to render icons to the user
    public Slots(SlotGameConfig cfg, IMyTextSurfaceProvider seat) {
        Config = cfg;
        _selectedIcons = new SelectedIcon[cfg.ReelsCount];
        _render = new Renderer(seat.GetSurface(0));
        _mutex = false;
    }
    
    /// Render a roll animation and select random icons for each reel
    public IEnumerator<Yield> Roll() {
        if(_mutex) {
            yield return Yield.Kill;
        } else {
            _mutex = true;
        }

        /// Roll random but unique icons for a time
        for(int i = 0; i < 20; ++i) {
            UniqueRandomIcons(0); 
            _render.DrawRoot(this);
            yield return Tasks.WaitMs(50);
        }

        // Now select each icon in quick succession
        for(int reel = 0; reel < Config.ReelsCount; ++reel) {
            _selectedIcons[reel] = new SelectedIcon() {
                Index = Config.RandomIcon(),
                Final = true,
            };

            for(int i = 0; i < 5; ++i) {
                UniqueRandomIcons(reel + 1);
                _render.DrawRoot(this);
                yield return Tasks.WaitMs(50);
            }
        }

        _render.DrawRoot(this);
        _mutex = false;
    }
    
    /// Generate a random unique icon for the given reel range
    void UniqueRandomIcons(int reelOffset) {
        for(int reel = reelOffset; reel < Config.ReelsCount; ++reel) {
            var selected = Config.RandomIcon();
            if(selected == _selectedIcons[reel].Index)
                selected = (selected + 1) % (Config.Slots.Length - 1);

            _selectedIcons[reel] = new SelectedIcon() {
                Index = selected,
                Final = false,
            };
        }
    }
    
    /// Render this slot machine's selected icons on a screen
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
