
using System;
using System.Text;
using Sandbox.ModAPI.Ingame;
using VRage.Game.GUI.TextPanel;
using VRageMath;

/// A renderer allowing a much better API for rendering complex objects with the sprite API
public struct Renderer {
    public IMyTextSurface _root;
    public MySpriteDrawFrame? _frame;
    public Vector2 Translation, Cursor; 
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
        
        var pos = sprite.Position.HasValue ? sprite.Position.Value : Vector2.Zero;
        pos *= ScaleFactor;
        pos.Rotate(Rotation);
        sprite.Position = pos + Translation;
        
        _frame.Value.Add(sprite);
    }
    
    /// Draw the given text string, scaling it to fit into the frame
    public void Draw(StringBuilder txt, float width, string font = "White") {
        var sz = _root.MeasureStringInPixels(txt, font, 1f);
        var scale = Math.Max(sz.X, sz.Y);
        Draw(new MySprite() {
            Type = SpriteType.TEXT,
            Data = txt.ToString(),
            FontId = "White",
            RotationOrScale = 20f * width / sz.Y,
            Position = new Vector2(0f, -(_root.MeasureStringInPixels(txt, font, 20f * width / sz.Y).Y / 2f) / ScaleFactor.Y),
        });
    }
    
    /// Draw the given text, scaling it to fit in the current frame
    public void Draw(string txt, float width) => Draw(new StringBuilder(txt), width);
    
    /// Draw the given IDrawable object using the transformations stored
    public void Draw<T>(T drawable) where T: IDrawable => drawable.Draw(this);
    
    public void SetColor(Color? c) => Color = c;
    public void Translate(float x, float y) => Translate(new Vector2(x, y));
    public void Translate(Vector2 pos) {
        pos *= ScaleFactor;
        pos.Rotate(Rotation);
        Translation += pos;
    }

    public void Scale(float scale) => Scale(new Vector2(scale, scale));
    public void Scale(Vector2 scale) => ScaleFactor *= scale;
    public void Rotate(float r) => Rotation += r;

    public Renderer Colored(Color? c) {
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
        _root = _root,
        _frame = _frame,
        Translation = Translation,
        ScaleFactor = ScaleFactor,
        Rotation = Rotation,
        Color = Color,
        Cursor = Cursor,
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
        Cursor = Vector2.Zero;
        Translate(new Vector2(1f, 1f));
        ScaleFactor = new Vector2(scale, scale) / 2f;
    }
    
    /// Render all sprites of the given root object
    public void DrawRoot(IDrawable root) {
        _frame = _root.DrawFrame();
        if(_frame.HasValue) {
            root.Draw(this);
            _frame.Value.Dispose();
            _frame = null;
        }
    }
}

public interface IDrawable {
    void Draw(Renderer r);
}
