
using System.Collections.Generic;
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
    
    const byte ZERO_INDEX = 2,
        NINE_INDEX = 11,
        ENTER_INDEX = 0,
        BACKSPACE_INDEX = 1;

    static MySprite DIGITBOX = new MySprite() {
        Type = SpriteType.TEXTURE,
        Data = "SquareHollow",
        Size = Vector2.One / 8f,

    };
    
    /// Create a new number pad with the given limit on entered digits
    public NumPad(IMyShipController seat, int max_digits) {
        _entry = new byte[max_digits];
        _private = false;
        _seat = seat;
        _selection = 0;
    }
    
    /// Render the number pad to the given renderer and accept input until a full number has been entered
    ///
    /// Returns the number that was entered by the user
    public IEnumerator<Yield> Input(Renderer r) {
        for(;;) {
            yield return Tasks.Async(_seat.ReadKey());
            var key = Tasks.Receive<Key>();
            switch(key) {
                case Key.W: _selection += 3; break;
                case Key.S: _selection -= 3; break;
                case Key.A: _selection -= 1; break;
                case Key.D: _selection += 1; break;
            }

            if(_selection > NINE_INDEX) {
                _selection = NINE_INDEX;
            }

            r.DrawRoot(this);
        }
    }

    public void Draw(Renderer r) {
        r.Scale(0.5f);
        r.Translate(1f, 1.5f);

        for(byte i = 0; i <= NINE_INDEX; ++i) {
            string txt;
            switch(i) {
                case ENTER_INDEX: txt = "Enter"; break;
                case BACKSPACE_INDEX: txt = "<"; break;
                default: txt = (i - ZERO_INDEX).ToString(); break;
            }

            r.Draw(DIGITBOX);
            r.Draw(txt);

            if(i % 3 == 0) {
                r.Translate(2f, -1f);
            } else {
                r.Translate(-1f, 0f);
            }

        }
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
public static class ShipControllerInputExtension {
    /// Read a single keypress from the move indicator 
    public static IEnumerator<Yield> ReadKey(this IMyShipController seat) {
        Vector3 original = seat.MoveIndicator;
        for(;;) {
            Key? key = null;
            Vector3 move = seat.MoveIndicator;
            if(move != original) {
                if(original.X == 0f & move.X != 0f) {
                    if(original.X > 0f)
                        key = Key.S;
                    else
                        key = Key.W;
                } else if(original.Y == 0f & move.Y != 0f) {
                    if(original.Y > 0f)
                        key = Key.D;
                    else
                        key = Key.A;
                } else if(original.Z == 0f && move.Z != 0f) {
                    if(original.Z > 0f)
                        key = Key.Space;
                    else
                        key = Key.C;
                }
            }

            if(key != null) {
                yield return Tasks.Return(key);
            }

            yield return Yield.Continue;
        }
    }
}
