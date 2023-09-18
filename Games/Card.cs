
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
        CARD_CORNER = -CARD_SZ / 2f,
        CORNER_ICON_PAD = new Vector2(0.05f, CORNER_ICON_SZ * 1.8f),
        CENTER_ICON_POS = new Vector2(0f, -(CARD_HEIGHT / 3f));

    static Color CARD_COLOR = new Color(201, 201, 185);
    static IDrawable[] ICONS = new IDrawable[] {
        new Heart(),
        new Diamond(),
        new Clover(),
    };

    public CardKind Kind;
    public CardNumeral Number;

    public Card(CardKind kind, CardNumeral num) {
        Kind = kind;
        Number = num;
    }

    public void Draw(Renderer r) {
        r.Scale(1f);
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

                IEnumerable<MyTuple<Vector2, bool>> center_positions = null;
               
                float ICON_STEP_Y = CARD_HEIGHT / 3f + CARD_HEIGHT / 16f;

                switch(Number) {
                    case CardNumeral.One: {
                        center_positions = new[] { MyTuple.Create(Vector2.Zero, false) };
                    } break;
                    case CardNumeral.Two: {
                        var off = new Vector2(0f, ICON_STEP_Y);
                        center_positions = new[] {
                            MyTuple.Create(off, true),
                            MyTuple.Create(-off, false),
                        };
                    } break;
                    case CardNumeral.Three:
                        var offset = new Vector2(0f, ICON_STEP_Y);
                        center_positions = new[] {
                            MyTuple.Create(offset, true),
                            MyTuple.Create(Vector2.Zero, false),
                            MyTuple.Create(-offset, false),
                        };
                    break;
                    default: {
                        var x = CARD_WIDTH / 4f - CARD_WIDTH / 16f;
                        var poslist = new List<MyTuple<Vector2, bool>>() {
                            MyTuple.Create(new Vector2(x, ICON_STEP_Y), true),
                            MyTuple.Create(new Vector2(-x, ICON_STEP_Y), true),
                            MyTuple.Create(new Vector2(x, -ICON_STEP_Y), false),
                            MyTuple.Create(new Vector2(-x, -ICON_STEP_Y), false),
                        };

                        center_positions = poslist;

                        switch(Number) {
                            case CardNumeral.Five:
                                poslist.Add(MyTuple.Create(Vector2.Zero, false));
                            break;

                            case CardNumeral.Six:
                            case CardNumeral.Seven:
                            case CardNumeral.Eight:
                                poslist.AddArray(new[] {
                                    MyTuple.Create(new Vector2(x, 0), false),
                                    MyTuple.Create(new Vector2(-x, 0), false)
                                });

                                if(Number == CardNumeral.Seven) {
                                    poslist.Add(
                                        MyTuple.Create(new Vector2(0, -ICON_STEP_Y * 3f / 4f), false)
                                    );
                                }
                            break;

                            case CardNumeral.Nine:
                            case CardNumeral.Ten:
                                var yOffset = ICON_STEP_Y / 3f;

                                poslist.AddArray(new[] {
                                    MyTuple.Create(new Vector2(x, yOffset), true),
                                    MyTuple.Create(new Vector2(-x, yOffset), true),
                                    MyTuple.Create(new Vector2(x, -yOffset), false),
                                    MyTuple.Create(new Vector2(-x, -yOffset), false),
                                });

                                if(Number == CardNumeral.Nine) {
                                    poslist.Add(
                                        MyTuple.Create(new Vector2(0, -ICON_STEP_Y * 3f / 4f), false)
                                    );
                                }
                            break;
                        }

                        if(Number == CardNumeral.Eight || Number == CardNumeral.Ten) {
                            poslist.AddArray(new [] {
                                MyTuple.Create(new Vector2(0, -ICON_STEP_Y * 3f / 4f), false),
                                MyTuple.Create(new Vector2(0, ICON_STEP_Y * 3f / 4f), true)
                            });
                        }
                    } break;
                }
                
                foreach(var icon_pos in center_positions) {
                    var drawing = r.Translated(icon_pos.Item1);
                    if(icon_pos.Item2) {
                        drawing.Rotate((float)Math.PI);
                    }
                    
                    drawing.Scale(ICON_SZ);

                    drawing.Draw(icon);
                }
            } break;
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
        Position = new Vector2(0f, 0.6f),
        RotationOrScale = (float)Math.PI,
        Color = Color.Red,
        Size = new Vector2(2f, 1.2f),
    };

    static MySprite TOPLEFT = new MySprite() {
        Type = SpriteType.TEXTURE,
        Data = "SemiCircle",
        Position = new Vector2(-0.48f, 0.05f),
        Color = Color.Red,
        Size = new Vector2(1f, 0.8f),
    };

    static MySprite TOPRIGHT = new MySprite() {
        Type = SpriteType.TEXTURE,
        Data = "SemiCircle",
        Position = new Vector2(0.48f, 0.05f),
        Color = Color.Red,
        Size = new Vector2(1f, 0.8f),
    };


    public void Draw(Renderer r) {
        r.Draw(BOTTOM); 
        r.Draw(TOPLEFT);
        r.Draw(TOPRIGHT);
    }
}

struct Clover: IDrawable {
    static MySprite TOPLEAF = new MySprite() {
        Type = SpriteType.TEXTURE,
        Data = "Circle",
        Position = new Vector2(0f, -0.45f),
        Size = new Vector2(0.9f, 0.9f),
        Color = Color.Red,
    };

    static MySprite TRUNK = new MySprite() {
        Type = SpriteType.TEXTURE,
        Data = "SquareSimple",
        Position = new Vector2(0f, 0.35f),
        Size = new Vector2(0.4f, 1f),
        Color = Color.Red,
    };

    public void Draw(Renderer r) {
        r.Draw(TRUNK);
        r
            .Translated(0f, -0.45f)
            .Draw(TOPLEAF);
        r.Translated(-0.45f, 0f).Draw(TOPLEAF);
        r.Translated(0.45f, 0f).Draw(TOPLEAF);
    }
}
