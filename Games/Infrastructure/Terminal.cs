
using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI.Ingame;
using VRage.Game.GUI.TextPanel;
using VRageMath;

/// A widget meant to be used to enter numeric PINs
public struct InputPad: IDrawable {
    /// Entered digits
    char[] _entry;

    int _enteredLen {
        get {
            int len = 0;
            for(;len < _entry.Length; ++len) {
                if(_entry[len] == INVALID_DIGIT) break;
            }
            return len;
        }
    }

    const char
        INVALID_DIGIT = (char)0,
        BACKSPACE_K = (char)8,
        SHIFTIN_K   = (char)15,
        SHIFTOUT_K  = (char)14,
        SPACE_K     = (char)7,
        ENTER_K     = (char)13;
    
    /// Lines to draw for the input grid
    static string[] _chars = {
        $"`1234567890-={BACKSPACE_K}",
        "qwertyuiop[]\\",
        $"asdfghjkl;'{ENTER_K}",
        $"{SHIFTIN_K}zxcvbnm,./",
        $"{SPACE_K}",
    };

    static string[] _shiftChars = {
        $"~!@#$%^&*()_+{BACKSPACE_K}",
        "QWERTYUIOP{}|",
        $"ASDFGHJKL:\"{ENTER_K}",
        $"{SHIFTOUT_K}ZXCVBNM<>?",
        $"{SPACE_K}"
    };

    string[] _currentChars {
        get { return _shift ? _shiftChars : _chars; }
    }
    
    /// If shift has been held down
    bool _shift;
    
    /// If entered digits should be hidden
    bool _private;
    
    /// Cockpit handle that can read input
    IMyShipController _seat;
    
    /// Currently selected box
    Vector2I _selection;
    
    /// Color used to indicate a pad is selected
    Color _selectedColor;

    static MySprite DIGITBOX = new MySprite() {
        Type = SpriteType.TEXTURE,
        Data = "SquareHollow",
        Position = Vector2.Zero,
        Size = Vector2.One,
        RotationOrScale = 0f,
    };
    
    /// Create a new number pad with the given limit on entered digits
    ///
    /// max_digits should not be 0
    public InputPad(IMyShipController seat, int max_digits, bool isPrivate, Color selectedColor) {
        _entry = new char[max_digits];
        for(int i = 0; i < max_digits; ++i) _entry[i] = INVALID_DIGIT;
        _private = isPrivate;
        _seat = seat;
        _selectedColor = selectedColor;
        _selection = Vector2I.Zero;
        _shift = false;
    }
    
    /// Get the text that the user has entered on the keyboard
    public string GetEntry() => new String(_entry).Substring(0, _enteredLen);
    
    /// Process a single key input from the user, updating internal selection state, returning `true` if the user has completing entering text
    public bool ProcessKey(Key key) {
       char select = _currentChars[_selection.Y][_selection.X];
       switch(key) {
           case Key.W: _selection.Y -= 1; break;
           case Key.S: _selection.Y += 1; break;
           case Key.A: _selection.X -= 1; break;
           case Key.D: _selection.X += 1; break;
           case Key.C: _shift = !_shift; break;
           case Key.Space: {
               switch(select) {
                   case ENTER_K: return true;
                   case BACKSPACE_K: {
                       for(int i = _entry.Length - 1; i >= 0; --i) {
                           if(_entry[i] != INVALID_DIGIT) {
                               _entry[i] = INVALID_DIGIT;
                               break;
                           }
                       }
                   } break;
                   case SHIFTIN_K: _shift = true; break;
                   case SHIFTOUT_K: _shift = false; break;
                   case SPACE_K: select = ' '; goto default;
                   default: {
                       for(int i = 0; i < _entry.Length; ++i) {
                           if(_entry[i] == INVALID_DIGIT) {
                               _entry[i] = select;
                               break;
                           }
                       }
                   } break;
               }
           } break;
       }
       
       if(_selection.Y < 0) _selection.Y = _currentChars.Length - 1;
       if(_selection.Y >= _currentChars.Length) _selection.Y = 0;
       if(_selection.X < 0) _selection.X = _currentChars[_selection.Y].Length - 1;
       if(_selection.X >= _currentChars[_selection.Y].Length) _selection.X = 0;

       return false;
    }

    static StringBuilder _digitsString = new StringBuilder();

    public void Draw(Renderer r) {
        r.Scaled(2f).Draw(DIGITBOX);
        r.Scale(2f / (float)(_chars[0].Length + 1));
        r.Translate(-_chars[0].Length / 2 + 0.5f, _chars.Length / 2);
        for(int y = _chars.Length - 1; y >= 0; --y) {
            int x = 0;
            for(; x < _chars[y].Length; ++x) {
                string txt;
                char current = _currentChars[y][x];
                switch(_currentChars[y][x]) {
                    case ENTER_K: txt = "->"; break;
                    case BACKSPACE_K: txt = "<"; break;
                    case SHIFTIN_K:
                    case SHIFTOUT_K:
                        txt = "^";
                        break;
                    case SPACE_K: txt = "[_]"; break;
                    default: txt = current.ToString(); break;
                }
                
                Color boxColor = (new Vector2I(x, y) == _selection) ? _selectedColor : r.Color;
                var boxDraw = r.Colored(boxColor);
                boxDraw.Draw(DIGITBOX);
                boxDraw.Draw(txt, 1f);

                r.Translate(1f, 0f);
            }


            r.Translate(-x, -1f);
        }

        r.Translate((_chars[0].Length + 1) / 2, 0f);
        _digitsString.Clear();
        for(int i = 0; i < _entry.Length; ++i) {
            if(_entry[i] == INVALID_DIGIT)
                break;
            else
                _digitsString.Append(_private ? '#' : _entry[i]);
        }
        
        r.Draw(_digitsString, 1, "Monospace");
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
    /// Read a single keypress from the move indicator 
    public static IEnumerator<Yield> ReadKey(IMyShipController seat) {
        for(;;) {
            var original = seat.MoveIndicator;
            yield return Yield.Continue;
            var key = GetKey(seat, original);

            if(key != null) {
                yield return Tasks.Return(key);
            }
        }
    }

    /// Read a keypresses until the task is killed and send them to the given channel
    public static IEnumerator<Yield> ReadKeys(IMyShipController seat, Channel<Key> tx) {
        for(;;) {
            var original = seat.MoveIndicator;
            yield return Yield.Continue;
            var key = GetKey(seat, original); 
            if(key != null) {
                tx.Send(key.Value);
            }
        }
    }

    private static Key? GetKey(IMyShipController seat, Vector3 original) {
        Vector3 move = seat.MoveIndicator;
        if(move != original) {
            if(original.Z == 0f && move.Z != 0f) {
                if(move.Z > 0f)
                    return Key.S;
                else
                    return Key.W;
            } else if(original.X == 0f && move.X != 0f) {
                if(move.X > 0f)
                    return Key.D;
                else
                    return Key.A;
            } else if(original.Y == 0f && move.Y != 0f) {
                if(move.Y > 0f)
                    return Key.Space;
                else
                    return Key.C;
            }
        }

        return null;
    }
}
