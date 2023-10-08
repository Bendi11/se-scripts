
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRageMath;

public class LoginTerminal {
    /// Grab input from this controllable block
    IMyShipController _input;
    /// Used to retrieve entity ID of the player
    IMySensorBlock _sensor;
    /// Stops multiple OnSensor calls in parallel
    bool _inProgress = false;

    Renderer _display;
    Menu _loginCreateSelection;
    InputPad _keyboard;

    public LoginTerminal(IMyCockpit station, int surface = 0) {
        _input = station;
        _display = new Renderer(station.GetSurface(0));
        _keyboard = new InputPad(station, 22, false, Color.Green);
        _loginCreateSelection = new Menu(
            station,
            new string[] {
                "Log In",
                "Create Account",
            }
        );
    }

    List<MyDetectedEntityInfo> _detected = new List<MyDetectedEntityInfo>();
    
    /// Detect a player in the seat and start the login process
    public IEnumerator<Yield> OnSensor() {
        try {
            if(_inProgress) {
                yield return Yield.Kill;
            }
            _inProgress = true;

            long entityId;

            for(;;) {
                _sensor.DetectedEntities(_detected);
                if(_detected.Count == 0) {
                    yield return Yield.Kill;
                }

                if(_detected.Count == 1 && _input.IsUnderControl) {
                    entityId = _detected[0].EntityId;
                    Log.Warn($"Entity {_detected[0].Name} in seat - {_detected[0].EntityId}");
                    break;
                }
            
                _detected.Clear();
                yield return Yield.Continue;
            }

            yield return Tasks.Async(ShipControllerInput.ReadKey(_input));
            Key key = Tasks.Receive<Key>();
        } finally {
            _inProgress = false;
        }
    }
}
