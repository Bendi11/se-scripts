
using System;
using Sandbox.ModAPI.Ingame;
using VRage.Game.GUI.TextPanel;
using VRageMath;

/// A renderer allowing a much better API for rendering complex objects with the sprite API
public struct Renderer {
    public IMyTextSurface _root;
    public MySpriteDrawFrame? _frame;
    public Vector2 Translation; 
    public Vector2 ScaleFactor;
    public float Rotation;
    public Color? Color;
    
    /// Get the size of the current drawing area
    public Vector2 Size {
        get {
            return _root.SurfaceSize / ScaleFactor;
        }
    }
    
    /// Render the given sprite utilizing the preset transformations stored in this `Renderer`
    public void Draw(MySprite sprite) {
        sprite.Alignment = TextAlignment.CENTER;
        if(Color.HasValue) {
            sprite.Color = Color.Value;
        }

        if(sprite.Size != null) {
            sprite.Size *= ScaleFactor;
        }

        if(sprite.Type != SpriteType.TEXT) {
            sprite.RotationOrScale += Rotation;
        }
        
        var pos = sprite.Position.Value;
        pos *= ScaleFactor;
        pos.Rotate(Rotation);
        sprite.Position = pos + Translation;
        
        _frame.Value.Add(sprite);
    }
    
    /// Draw the given IDrawable object using the transformations stored
    public void Draw(IDrawable drawable) => drawable.Draw(this);
    
    public void SetColor(Color c) => Color = c;
    public void Translate(float x, float y) => Translate(new Vector2(x, y));
    public void Translate(Vector2 pos) {
        pos *= ScaleFactor;
        pos.Rotate(Rotation);
        Translation += pos;
    }

    public void Scale(float scale) => Scale(new Vector2(scale, scale));
    public void Scale(Vector2 scale) => ScaleFactor *= scale;
    public void Rotate(float r) => Rotation += r;

    public Renderer Colored(Color c) {
        var me = Push();
        me.SetColor(c);
        return me;
    }

    public Renderer Translated(Vector2 pos) {
        var me = Push();
        me.Translate(pos);
        return me;
    }

    public Renderer Translated(float x, float y) => Translated(new Vector2(x, y));
    public Renderer Scaled(Vector2 scale) {
        var me = Push();
        me.Scale(scale);
        return me;
    }
    public Renderer Scaled(float scale) => Scaled(new Vector2(scale, scale));
    public Renderer Rotated(float r) {
        var me = Push();
        me.Rotate(r);
        return me;
    }

    /// Add a new renderer layer, used to apply temporary transformations before restoring
    public Renderer Push() => new Renderer() {
        _frame = _frame,
        Translation = Translation,
        ScaleFactor = ScaleFactor,
        Rotation = Rotation,
        Color = Color,
    };
    
    /// Create a new Renderer from the given text surface
    public Renderer(IMyTextSurface root) {
        _root = root;
        Translation = (root.TextureSize - root.SurfaceSize) / 2f;
        var scale = Math.Min(root.SurfaceSize.X, root.SurfaceSize.Y);
        ScaleFactor = root.SurfaceSize / 2f;
        Rotation = 0;
        Color = null;
        _frame = null;
        Translate(new Vector2(1f, 1f));
        ScaleFactor = new Vector2(scale, scale) / 2f;
    }
    
    /// Render all sprites of the given root object
    public void DrawRoot(IDrawable root) {
        _frame = _root.DrawFrame();
        root.Draw(this);
        _frame.Value.Dispose();
        _frame = null;
    }
}



public interface IDrawable {
    void Draw(Renderer r);
}
