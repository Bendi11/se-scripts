
using System.Collections.Generic;
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

    /// Returns the index of the menu option that the user selected
    public IEnumerator<Yield> Select(Renderer r) {
        for(;;) {
            r.DrawRoot(this);
            yield return Yield.Continue;
            yield return Tasks.Async(ShipControllerInput.ReadKey(_input));
            Key key = Tasks.Receive<Key>();
            switch(key) {
                case Key.W: _selected += 1; break;
                case Key.S: _selected -= 1; break;
                case Key.Space: yield return Tasks.Return(_selected); break;
            }

            if(_selected < 0)
                _selected = _choices.Length - 1;
            if(_selected >= _choices.Length)
                _selected = 0;
        }
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
