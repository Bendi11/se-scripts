
using Sandbox.ModAPI.Ingame;
using VRageMath;

/// A list of options to select
public struct Menu: IDrawable {
    string[] _choices;
    int _selected;
    IMyShipController _input;

    public Menu(IMyShipController input, string[] choices) {
        _choices = choices;
        _selected = 0;
        _input = input;
    }
    
    /// Get the currently selected menu option
    public int GetValue() => _selected;

    /// Process a keystroke from the user, returning true if they have finished selecting an option
    public bool Input(Key key) {
       switch(key) {
           case Key.W: _selected += 1; break;
           case Key.S: _selected -= 1; break;
           case Key.Space: return true;
       }

       if(_selected < 0)
           _selected = _choices.Length - 1;
       if(_selected >= _choices.Length)
           _selected = 0;

       return false;
    }

    public void Draw(Renderer r) {
        var color = r.Color;
        r.Scale(new Vector2(1f, (float)(_choices.Length + 1) / 2f));
        r.Translate(0f, 0.5f);
        for(int i = 0; i < _choices.Length; ++i) {
            if(i == _selected) {
                r.SetColor(Color.Green);
            }
            r.Draw(_choices[i], 1f);
            r.Translate(0f, -1f);
            r.SetColor(color);
        }
    }
}
