
using System;
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
    public int[] SelectedIcons;
    
    /// Create a new slots instance from the map it will use to render icons to the user
    public Slots(SlotGameConfig cfg) {
        Config = cfg;
        SelectedIcons = new int[cfg.ReelsCount];
    }

    public void Roll() {
        for(int i = 0; i < SelectedIcons.Length; ++i) {
            SelectedIcons[i] = Config.RandomIcon();
        }
    }
    
    /// Generate a random icon for the 
    public void Draw(Renderer r) {
        r._root.ScriptBackgroundColor = Config.Background;
        r.Translate(-r.Size.X / 2f, 0f);
        float xStep = r.Size.X / (float)Config.ReelsCount;
        float iconSize = xStep / 4f;
        float x = xStep / 2f;
        int reel = 0;
        foreach(int selected in SelectedIcons) {
            var icon = r.Push();
            icon.Translate(x, 0f);
            icon.Scale(iconSize);
            icon.Draw(Config.Slots[selected]);
            reel += 1;
            x += xStep;
        } 
    }
}

public struct DirectEnumMap<E, V> {
    public V[] Items;
    
    /// Create a new mapping from enum variants to mapped values,
    /// in the same order as they were defined
    public DirectEnumMap(params V[] mapped) {
        var vals = Enum.GetValues(typeof(E)); 
        if(vals.Length != mapped.Length)
            throw new Exception("Invalid EnumMap constructor: Not enough or too many items");
        
        Items = mapped;
    }

    public V Get(E variant) => Items[(int)(object)variant];
}
