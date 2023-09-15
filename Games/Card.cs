
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game.GUI.TextPanel;
using VRageMath;

enum CardKind {
    Heart = 0,
    Diamond = 1,
    Clover = 2,
    Spade = 3,
    COUNT = 4,
}

public enum CardNumeral {
    One = 1,
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Nine,
    Ten,
    Jack,
    Queen,
    King,
    Ace,
}

struct Card: IDrawable {
    const float
        CARD_WIDTH = 1.4382022f,
        CARD_HEIGHT = 2f,
        CORNER_ICON_SZ = 0.1f,
        ICON_SZ = 0.15f;
    
    static Vector2  CARD_SZ = new Vector2(CARD_WIDTH, CARD_HEIGHT),
        CARD_CORNER = -(CARD_SZ / 2f),
        CORNER_ICON_PAD = new Vector2(0.05f, CORNER_ICON_SZ * 1.8f),
        CENTER_ICON_POS = new Vector2(0f, -(CARD_HEIGHT / 3f));

    static Color CARD_COLOR = new Color(201, 201, 185);
    static IDrawable[] ICONS = new IDrawable[] {
        new Heart(),
        new Diamond(),
    };

    public CardKind Kind;
    public CardNumeral Number;

    public Card(CardKind kind, CardNumeral num) {
        Kind = kind;
        Number = num;
    }

    public void Draw(Renderer r) {
        r.Draw(new MySprite() {
            Type = SpriteType.TEXTURE,
            Data = "SquareSimple",
            Position = new Vector2(0f, 0f),
            Size = new Vector2(CARD_WIDTH, CARD_HEIGHT),
            Color = CARD_COLOR,
        });

        var icon = ICONS[(int)Kind];

        string numeral;

        switch(Number) {
            case CardNumeral.Jack: numeral = "J"; break;
            case CardNumeral.Queen: numeral = "Q"; break;
            case CardNumeral.King: numeral = "K"; break;
            case CardNumeral.Ace: numeral = "A"; break;
            default: {
                numeral = ((int)Number).ToString();

                var center_positions = new List<MyTuple<Vector2, bool>>();
                
                switch(Number) {
                    case CardNumeral.One: {


                    }
                }
            break;
        }

        //Corner text + symbol

        Text txt = new Text(numeral);
        
        var txt_sz = txt.Size(r);
        var txt_translate = CARD_CORNER + new Vector2(CORNER_ICON_PAD.X, 0f) + (txt_sz / 2f);
        var vert_pad = new Vector2(0f, txt_sz.Y / 2f);

        r.Push()
            .Translated(txt_translate - vert_pad)
            .Draw(txt);

        r.Push()
            .Translated(-txt_translate - vert_pad)
            .Draw(txt);
        
        var corner_icon_translate = txt_translate + new Vector2(0f, CORNER_ICON_PAD.Y);

        r.Push()
            .Translated(corner_icon_translate)
            .Scaled(CORNER_ICON_SZ)
            .Draw(icon);

        r.Push()
            .Translated(-corner_icon_translate)
            .Scaled(CORNER_ICON_SZ)
            .Rotated((float)Math.PI)
            .Draw(icon);
    }
}

struct Diamond: IDrawable {
    static MySprite SPRITE =  new MySprite() {
        Type = SpriteType.TEXTURE,
        Alignment = TextAlignment.CENTER,
        Data = "SquareSimple",
        Position = Vector2.Zero,
        Size = Vector2.One,
        RotationOrScale = (float)Math.PI / 4f,
        Color = Color.Red,
    };
    
    public void Draw(Renderer r) {
        r.Draw(SPRITE);
    }
}

struct Heart: IDrawable {
    static MySprite BOTTOM = new MySprite() {
        Type = SpriteType.TEXTURE,
        Data = "Triangle",
        Position = new Vector2(0f, -0.6f),
        RotationOrScale = (float)Math.PI,
        Color = Color.Red,
        Size = new Vector2(2f, 1.2f),
    };

    static MySprite TOPLEFT = new MySprite() {
        Type = SpriteType.TEXTURE,
        Data = "RightTriangle",
        Position = new Vector2(-0.5f, 0.4f),
        Color = Color.Red,
        Size = new Vector2(1f, 0.8f),
    };

    static MySprite TOPRIGHT = new MySprite() {
        Type = SpriteType.TEXTURE,
        Data = "RightTriangle",
        Position = new Vector2(0.5f, 0.4f),
        Color = Color.Red,
        RotationOrScale = -(float)Math.PI / 2f,
        Size = new Vector2(0.8f, 1f),
    };


    public void Draw(Renderer r) {
        r.Draw(BOTTOM); 
        r.Draw(TOPLEFT);
        r.Draw(TOPRIGHT);
    }
}
