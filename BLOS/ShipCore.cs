using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;


/// <summary>
/// <para>
/// BLOS General Purpose Flight Controller
/// An attempt at a one-size-fits-all flight controller script with drivers for any device,
/// making it easy to create and pilot any combination of ship devices with only a few flags.
/// </para>
public sealed class ShipCore {
    public IMyTerminalBlock Ref;
    public IMyGridTerminalSystem GTS;

    /// A map of entity IDs to the contact data they correspond to
    Dictionary<long, ContactData> _contacts = new Dictionary<long, ContactData>();
    /// A collection of all sensors on the craft
    List<ISensorDevice> _sensors = new List<ISensorDevice>();
    /// A collection of all (at least 1) movement and orientation controller on the craft
    List<IMovementControllerDevice> _movement = new List<IMovementControllerDevice>();
    /// The selected movement and orientation controller
    IMovementControllerDevice _move;
    /// A list of all control input devices
    List<IMyShipController> _inputs;
    /// The ship controller to use for movement and orientation controls
    IMyShipController MovementInput;

    
    /// The last timestamp that the ship's mass was calculated at
    double _lastShipMassUpdateSec = 0;
    /// Re-calculate the ship's mass every n seconds
    const double UPDATE_SHIP_MASS_SECS = 15;
    
    /// Get the consistently-updated mass of the ship
    public float ShipMass;

    public static ShipCore I = null;
    
    /// Create a new flight controller using the device drivers given
    public static void Create(IMyGridTerminalSystem gts, IMyTerminalBlock reference, params IDevice[] devices) {
        I = new ShipCore();
        I.Ref = reference;
        I.GTS = gts;
        I.Init(devices);
    }
    
    /// <summary>
    /// Add all devices to the flight controller and load configuration values from the customdata of the programmable
    /// block
    /// </summary>
    void Init(IDevice[] devices) {
        foreach(var device in devices) {
            if(device is ISensorDevice) { _sensors.Add((ISensorDevice)device); }
            if(device is IMovementControllerDevice) {
                _movement.Add((IMovementControllerDevice)device);
                if(_move == null) { _move = (IMovementControllerDevice)device; }
            }
        }

        Process.Spawn(_move.Control());
        
        GTS.GetBlocksOfType(_inputs);
        MovementInput = _inputs[0];
    }

    public struct Hooks {
    
    }
}
