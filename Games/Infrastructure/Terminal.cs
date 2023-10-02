
using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI.Ingame;
using VRage.Game.GUI.TextPanel;
using VRageMath;

/// A widget meant to be used to enter numeric PINs
public struct NumPad: IDrawable {
    /// Entered digits
    byte[] _entry;
    
    /// If entered digits should be hidden
    bool _private;
    
    /// Cockpit handle that can read input
    IMyShipController _seat;
    
    /// Currently selected box
    byte _selection;
    
    /// Color used to indicate a pad is selected
    Color _selectedColor;
    
    const byte ZERO_INDEX = 2,
        NINE_INDEX = 11,
        ENTER_INDEX = 0,
        BACKSPACE_INDEX = 1,
        INVALID_DIGIT = 255;

    static MySprite DIGITBOX = new MySprite() {
        Type = SpriteType.TEXTURE,
        Data = "SquareHollow",
        Position = Vector2.Zero,
        Size = Vector2.One,
        Color = Color.White,
        RotationOrScale = 0f,
    };
    
    /// Create a new number pad with the given limit on entered digits
    ///
    /// max_digits should not be 0
    public NumPad(IMyShipController seat, int max_digits, bool isPrivate, Color selectedColor) {
        _entry = new byte[max_digits];
        for(int i = 0; i < max_digits; ++i) _entry[i] = INVALID_DIGIT;
        _private = isPrivate;
        _seat = seat;
        _selection = 0;
        _selectedColor = selectedColor;
        _selection = ZERO_INDEX;
    }
    
    /// Render the number pad to the given renderer and accept input until a full number has been entered
    ///
    /// Returns the number that was entered by the user
    public IEnumerator<Yield> Input(Renderer r) {
        for(;;) {
            r.DrawRoot(this);
            yield return Yield.Continue;
            yield return Tasks.Async(ShipControllerInput.ReadKey(_seat));
            var key = Tasks.Receive<Key>();
            switch(key) {
                case Key.W: _selection += 3; break;
                case Key.S: _selection -= 3; break;
                case Key.A: _selection += 1; break;
                case Key.D: _selection -= 1; break;
                case Key.Space: {
                    switch(_selection) {
                        case ENTER_INDEX: yield return Tasks.Return(GetEntry()); break;
                        case BACKSPACE_INDEX: {
                            for(int i = _entry.Length - 1; i >= 0; --i) {
                                if(_entry[i] != INVALID_DIGIT) {
                                    _entry[i] = INVALID_DIGIT;
                                    break;
                                }
                            }
                        } break;
                        default: {
                            for(int i = 0; i < _entry.Length; ++i) {
                                if(_entry[i] == INVALID_DIGIT) {
                                    _entry[i] = (byte)(_selection - ZERO_INDEX);
                                    break;
                                }
                            }
                        } break;
                    }
                } break;
            }

            if(_selection > NINE_INDEX) {
                _selection = NINE_INDEX;
            }
        }
    }
    
    int GetEntry() {
        int num = 0;
        for(int dec = 0; dec < _entry.Length; ++dec) {
            if(_entry[dec] != INVALID_DIGIT) {
                num += _entry[dec] * (int)Math.Pow(10, dec);
            }
        }

        return num;
    }


    static StringBuilder _digitsString = new StringBuilder();

    public void Draw(Renderer r) {
        r.Scale(0.4f);
        r.Translate(1f, 2);

        for(byte i = 0; i <= NINE_INDEX; ++i) {
            string txt = "TEST";
            switch(i) {
                case ENTER_INDEX: txt = "Enter"; break;
                case BACKSPACE_INDEX: txt = "<"; break;
                default: txt = (i - ZERO_INDEX).ToString(); break;
            }
            
            Color? boxColor = (i == _selection) ? r.Color : _selectedColor;
            var boxDraw = r.Colored(boxColor);
            boxDraw.Draw(DIGITBOX);
            boxDraw.Draw(txt, 1f);

            if((i + 1) % 3 == 0) {
                r.Translate(2f, -1f);
            } else {
                r.Translate(-1f, 0f);
            }
        }

        r.Translate(1f, -1f);
        _digitsString.Clear();
        for(int i = 0; i < _entry.Length; ++i) {
            if(_entry[i] == INVALID_DIGIT)
                _digitsString.Append('_');
            else
                _digitsString.Append((char)(_private ? '#' : _entry[i] + '0'));
        }
        
        r.Draw(_digitsString, 3);
    }
}

/// Keys that may be entered by the user
public enum Key {
    W,
    A,
    S,
    D,
    Space,
    C,
}

/// A keyboard that can read from a cockpit's inputs to discern the keys that are pressed on a KEYBOARD - 
/// no promises on a controller
public static class ShipControllerInput {
    /// Gets the movement indicator aligned to the seat's orientation
    private static Vector3 GetTrueMoveIndicator(IMyShipController seat) {
        Matrix gor;
        seat.Orientation.GetMatrix(out gor);
        return Vector3.TransformNormal(seat.MoveIndicator, gor);
    }

    /// Read a single keypress from the move indicator 
    public static IEnumerator<Yield> ReadKey(IMyShipController seat) {
        for(;;) {
            Key? key = null;
            var original = seat.MoveIndicator;//GetTrueMoveIndicator(seat);
            yield return Yield.Continue;
            Vector3 move = seat.MoveIndicator;//GetTrueMoveIndicator(seat);
            if(move != original) {
                if(original.Z == 0f && move.Z != 0f) {
                    if(move.Z > 0f)
                        key = Key.S;
                    else
                        key = Key.W;
                } else if(original.X == 0f && move.X != 0f) {
                    if(move.X > 0f)
                        key = Key.D;
                    else
                        key = Key.A;
                } else if(original.Y == 0f && move.Y != 0f) {
                    if(move.Y > 0f)
                        key = Key.Space;
                    else
                        key = Key.C;
                }
            }

            if(key != null) {
                yield return Tasks.Return(key);
            }
        }
    }
}
