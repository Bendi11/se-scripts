using System.Collections.Generic;


/// <summary>
/// <para>
/// BLOS General Purpose Flight Controller
/// An attempt at a one-size-fits-all flight controller script with drivers for nearly any device,
/// making it easy to create and pilot any combination of ship devices with only a few flags.
/// </para>
/// 
/// <para>
/// Code size and complexity are controlled by compiler defines, used to reduce script workload when
/// a ship does not require driver support for a device.
/// The following compiler defines will enable / disable functionality of the FC:
/// </para>
///
/// <list type = "table">
/// <listheader>
///     <term>BLOS Compiler Defines</term>
///     <description>Flags to enable for driver support</description>
/// </listheader>
/// <item>
///     <term>BLOS_DRVR_ALL</term>
///     <description>Enables all drivers for every part, usefull for testing a design without compiling a custom firmware</description>
/// </item>
/// </list>
/// </summary>
public sealed class ShipCore {
    /// A map of entity IDs to the contact data they correspond to
    Dictionary<long, ContactData> _contacts = new Dictionary<long, ContactData>();
    /// A collection of all sensors on the craft
    List<ISensorDevice> _sensors = new List<ISensorDevice>();
    /// A collection of all (at least 1) movement and orientation controller on the craft
    List<IMovementControllerDevice> _movement = new List<IMovementControllerDevice>();
    /// The selected movement and orientation controller
    IMovementControllerDevice _move;

    public static ShipCore I = null;
    
    /// Create a new flight controller using the device drivers given
    public static void Create(params IDevice[] devices) {
        I = new ShipCore();
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
    }

    public struct Hooks {
    
    }
}
