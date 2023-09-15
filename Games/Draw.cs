
using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRage.Game.GUI.TextPanel;
using VRageMath;

public struct Renderer {
    public MySpriteDrawFrame _frame;
    Vector2 _offset; 
    Vector2 _scale;
    float _rot;

    public void Draw(MySprite sprite) {
        sprite.Alignment = TextAlignment.CENTER;

        if(sprite.Size != null) {
            sprite.Size *= _scale;
        }

        if(sprite.Type != SpriteType.TEXT) {
            sprite.RotationOrScale += _rot;
        } else {
            sprite.RotationOrScale *= _scale.Length() / 16;
        }
        
        var pos = sprite.Position.Value;
        pos *= _scale * new Vector2(1f, -1f);
        pos.Rotate(_rot);
        sprite.Position = pos + _offset;
        
        
        _frame.Add(sprite);
    }

    public void Draw(IDrawable drawable) => drawable.Draw(this);

    public void Translate(Vector2 pos) {
        pos *= _scale;
        pos.Rotate(_rot);
        _offset += pos;
    }
    public void Scale(Vector2 scale) => _scale *= scale;
    public void Scale(float scale) => _scale *= scale;
    public void Rotate(float r) => _rot += r;

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
        _offset = _offset,
        _scale = _scale,
        _rot = _rot
    };

    public Renderer(IMyTextSurface root) {
        _frame = root.DrawFrame();
        _offset = (root.TextureSize - root.SurfaceSize) / 2f;
        _scale = root.SurfaceSize / 2f;
        _rot = 0;
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
