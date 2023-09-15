
using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRage.Game.GUI.TextPanel;
using VRageMath;

public struct Renderer {
    public IMyTextSurface _root;
    public MySpriteDrawFrame _frame;
    public Vector2 Translation; 
    public Vector2 ScaleFactor;
    public float Rotation;

    public void Draw(MySprite sprite) {
        sprite.Alignment = TextAlignment.CENTER;

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
        
        _frame.Add(sprite);
    }

    public void Draw(IDrawable drawable) => drawable.Draw(this);

    public void Translate(Vector2 pos) {
        pos *= ScaleFactor;
        pos.Rotate(Rotation);
        Translation += pos;
    }
    public void Scale(Vector2 scale) => ScaleFactor *= scale;
    public void Scale(float scale) => ScaleFactor *= scale;
    public void Rotate(float r) => Rotation += r;

    public Renderer Translated(Vector2 pos) {
        var me = Push();
        me.Translate(pos);
        return me;
    }
    public Renderer Scaled(Vector2 scale) {
        var me = Push();
        me.Scale(scale);
        return me;
    }
    public Renderer Scaled(float scale) {
        var me = Push();
        me.Scale(scale);
        return me;
    }
    public Renderer Rotated(float r) {
        var me = Push();
        me.Rotate(r);
        return me;
    }


    public Renderer Push() => new Renderer() {
        _frame = _frame,
        Translation = Translation,
        ScaleFactor = ScaleFactor,
        Rotation = Rotation
    };

    public Renderer(IMyTextSurface root) {
        _root = root;
        _frame = root.DrawFrame();
        Translation = (root.TextureSize - root.SurfaceSize) / 2f;
        ScaleFactor = root.SurfaceSize / 2f;
        Rotation = 0;
        Translate(new Vector2(1f, 1f));
    }

    public void Dispose() => _frame.Dispose();
    public MySpriteCollection Collection() => _frame.ToCollection();
}

/// <summary>
/// Top-level interface for rendering sprites + updating sprite surfaces without recomputing layout
/// </summary>
public struct Display {
    IMyTextSurface _surface;
    IDrawable _root;
    
    /// <summary>
    /// Create a new display interface from the surface that will be drawn to and the root
    /// widget to render
    /// </summary>
    public Display(IMyTextSurface surface, IDrawable root) {
        _surface = surface;
        _root = root;
        _surface.ContentType = ContentType.SCRIPT;
        _surface.Script = "";
    }

    public void Update() {
        var render = new Renderer(_surface);
        _root.Draw(render);
        render.Dispose();
    }
}

public interface IDrawable {
    void Draw(Renderer r);
}
