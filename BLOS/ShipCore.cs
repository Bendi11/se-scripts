using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRageMath;


/// <summary>
/// <para>
/// BLOS General Purpose Flight Controller
/// An attempt at a one-size-fits-all flight controller script with drivers for any device,
/// making it easy to create and pilot any combination of ship devices with only a few flags.
/// </para>
public partial class ShipCore {
    public IMyTerminalBlock Ref;
    public IMyGridTerminalSystem GTS;
    public IMyGridProgramRuntimeInfo Runtime;

    /// A map of entity IDs to the contact data they correspond to
    Dictionary<long, ContactData> _contacts = new Dictionary<long, ContactData>();
    /// A collection of all sensors on the craft
    List<ISensorDevice> _sensors = new List<ISensorDevice>();
    /// All weapons aboard the ship
    List<IWeaponDevice> _weapons = new List<IWeaponDevice>();
    /// A list of all control input devices
    List<IMyShipController> _inputs = new List<IMyShipController>();
    
    /// The last timestamp that the ship's mass was calculated at
    double _lastShipMassUpdateSec = 0;
    /// Re-calculate the ship's mass every n seconds
    const double UPDATE_SHIP_MASS_SECS = 15;
    
    /// Get the consistently-updated mass of the ship
    public float ShipMass;

    public static ShipCore I;

    public IEnumerable<ContactData> Contacts {
        get {
            return _contacts.Values;
        }
    }
    
    /// Create a new flight controller using the device drivers given
    public static void Create(IMyGridTerminalSystem gts, IMyTerminalBlock reference, IMyGridProgramRuntimeInfo rt, params IDevice[] devices) {
        I = new ShipCore();
        I.Ref = reference;
        I.GTS = gts;
        I.Runtime = rt;

        I._insLimit = rt.MaxInstructionCount;
        I.Init(devices);
    }
    
    /// <summary>
    /// Add all devices to the flight controller and load configuration values from the customdata of the programmable
    /// block
    /// </summary>
    void Init(IDevice[] devices) {
        foreach(var device in devices) {
            device.Init();
            if(device is ISensorDevice) { _sensors.Add((ISensorDevice)device); }
        }
        
        GTS.GetBlocksOfType(_inputs);

        //Spawn(_move.Control());
        //Spawn(RunLoop());
    }

    IEnumerator<PS> RunLoop() {
        try {
           yield return PS.Execute; 
        } finally {
            
        }
    }
}
