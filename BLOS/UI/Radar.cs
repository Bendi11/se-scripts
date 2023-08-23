
using System;
using Sandbox.ModAPI.Ingame;
using VRage.Game.GUI.TextPanel;
using VRageMath;

public class Radar: Screen {
    Vector2 _screenPos = Vector2.One / 2;
    RectangleF _viewPort;
    float _scale;
    long _tracked;

    public Radar(IMyTextSurface surface) {
        Surface = surface;
    }

    static Vector2 ICON_SZ = Vector2.One * 32;

    static MySprite 
        CONTACT = new MySprite() {
            Type = SpriteType.TEXTURE,
            Data = "Circle",
            Size = Vector2.One * 16,
        },
        ME = new MySprite() {
            Type = SpriteType.TEXTURE,
            Data = "AH_BoreSight",
            Size = ICON_SZ,
            RotationOrScale = 3f * (float)Math.PI / 2f,
        };

    static MySprite CreateSprite(string name, Vector2 pos) => new MySprite() {
        Type = SpriteType.TEXTURE,
        Data = name,
        Size = Vector2.One * 16,
        Position = pos,
        RotationOrScale = 0f,
    };

    void Draw(MySpriteDrawFrame frame, MySprite sprite, Vector2 pos, float color) {
        pos *= _viewPort.Size;
        sprite.Position = Vector2.Clamp(pos, Vector2.Zero, _viewPort.Size) + _viewPort.Position;
        sprite.Color = new Color(color, color, color);
        frame.Add(sprite);
    }

    public override void Render() {
        Surface.ContentType = ContentType.SCRIPT;
        Surface.Script = "";
        Surface.BackgroundColor = Color.Black;
        _viewPort = new RectangleF(
            (Surface.TextureSize - Surface.SurfaceSize) / 2f,
            Surface.SurfaceSize
        );
        _scale = Math.Min(_viewPort.Size.X, _viewPort.Size.Y);

        var frame = Surface.DrawFrame();
        
        frame.Add(ME);

        foreach(var contact in ShipCore.I.Contacts) {
            Vector3 pos = contact.Entity.Position - ShipCore.I.Ref.WorldMatrix.Translation;
            pos = Vector3.TransformNormal(pos, Matrix.Transpose(ShipCore.I.Ref.WorldMatrix));
            pos /= 50 * 2;
            Draw(frame, CONTACT, new Vector2(pos.X + 0.5f, pos.Z + 0.5f), (15f - (float)(ShipCore.I.Time - contact.LastPing)) / 15f);
        }

        frame.Dispose();
    }

    public override bool Handle(Input i) {
        return true;
    }

    private float AngleBetween(Vector2 a, Vector2 b) {
        a.Normalize();
        b.Normalize();
        var angle = (float)Math.Acos(Vector2.Dot(a, b));
        if(angle == 0) { return 0.00001F; }
        if(float.IsNaN(angle)) { return -0.00001F; }
        else { return angle; }
    }
}
