
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
    Channel<Key> _inputChannel;
    Task _inputProcess;
    
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
            
            _inputChannel = new Channel<Key>(Tasks.Current);
            _inputProcess = Tasks.Spawn(ReadInputTask());
            _inputProcess.Waiter = Tasks.Current;
            
            yield return _inputChannel.AwaitReady();
            Key key = _inputChannel.Receive();
        } finally {
            _inProgress = false;
            if(_inputProcess != null) {
                Tasks.Exit(_inputProcess);
            }

            _inputProcess = null;
            _inputChannel = null;
        }
    }
    
    /// Read keys from the keyboard in a loop until the task is killed
    public IEnumerator<Yield> ReadInputTask() {
        var readKeys = new Task(ShipControllerInput.ReadKeys(_input, _inputChannel));
        readKeys.Waiter = Tasks.Current;

        for(;;) {
            yield return Yield.Continue;
            Tasks.TickManual(readKeys);
            if(!_input.IsUnderControl) {
                yield return Yield.Exit;
            }
        }
    }
}
