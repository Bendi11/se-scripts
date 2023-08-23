using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
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
    
    Cockpit _cockpit;

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

    public void UpdateContact(MyDetectedEntityInfo ent) {
        _contacts[ent.EntityId] = new ContactData() {
            Entity = ent,
            LastPing = Time,
        };
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

        var cockpits = new List<IMyCockpit>();
        GTS.GetBlocksOfType(cockpits, (block) => MyIni.HasSection(block.CustomData, Cockpit.SECTION));
        _cockpit = Cockpit.ReadConfig(cockpits[0]);
        
        Ref = cockpits[0];
        ShipMass = cockpits[0].CalculateShipMass().TotalMass;

        foreach(var sensor in _sensors) {
            Spawn(sensor.ScanProcess());
        }

        Spawn(_cockpit.Process()); 
        Spawn(RunLoop());
    }

    double _lastUpdateContactsSec;
    const double UPDATE_CONTACTS_PERIOD_SEC = 5;
    const double DROP_CONTACT_TIME = 15;

    IEnumerator<PS> RunLoop() {
        try {
            for(;;) {
                if(Time - _lastShipMassUpdateSec >= UPDATE_SHIP_MASS_SECS) {
                    _lastShipMassUpdateSec = Time;
                    ShipMass = _cockpit.Seat.CalculateShipMass().TotalMass;
                }

                if(Time - _lastUpdateContactsSec >= UPDATE_CONTACTS_PERIOD_SEC) {
                    _lastUpdateContactsSec = Time;
                    foreach(var contact in _contacts.Where(kv => Time - kv.Value.LastPing >= DROP_CONTACT_TIME).ToList()) {
                        _contacts.Remove(contact.Key);
                    }
                }
                yield return PS.Execute; 
            }
        } finally {
            
        }
    }
}
