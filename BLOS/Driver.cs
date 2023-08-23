using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRageMath;

/// A plugin for the flight computer that can provide a multitude of services
public interface IDevice {
    /// Initialize this device by collecting all block references and reading configuration from the <c>ShipCore</c>
    void Init();
    /// Get a user-displayable name for this plugin
    string Name();
    /// Get a unique ID for this plugin
    string ID();
}

/// Targeting modes for an <c>ISensorPlugin</c>
public enum SensorTargetMode {
    /// Point to the given world position and scan for contacts
    WorldPosition,
    /// Scan in the given world direction for new contacts
    WorldDirection,
    /// Scan in the given direction for new contacts
    LocalDirection,
    /// Track an entity using their constantly-updated position
    Entity,
}

/// A specialization of an IDevice that provides target acquisition and tracking for the flight controller
public interface ISensorDevice : IDevice {
    /// Return a process that will sweep the sensor in the described area or track a single target
    IEnumerator<PS> ScanProcess();
    /// Set to a value between 0 and 1 to indicate the desired scan radius for sensors that
    /// 'sweep'
    float ScanSize { get; set; }
    /// Current targeting mode
    SensorTargetMode Mode { get; set; }
    /// A vector that can indicate the position of an SPI or the direction of an SPI
    Vector3D VSPI { get; set; }
    /// An entity ID to a tracked contact that can be used for STT
    long CSPI { get; set; }
}

/// A specialization of an IDevice that provides unguided weapons functionality
public interface IWeaponDevice: IDevice {

}

public struct SensorBlockDevice: ISensorDevice {
    List<IMySensorBlock> _sensors;

    public IEnumerator<PS> ScanProcess() {
        var detected = new List<MyDetectedEntityInfo>();
        for(;;) {
            foreach(var sensor in _sensors) {
                detected.Clear();
                sensor.DetectedEntities(detected);
                foreach(var ent in detected) {
                    ShipCore.I.UpdateContact(ent);
                }
                yield return PS.Execute;
            }
        }
    }

    public float ScanSize { get; set; }
    public Vector3D VSPI { get; set; }
    public long CSPI { get; set; }
    public SensorTargetMode Mode { get; set; }

    public string ID() => "SENSORS";
    public string Name() => "Sensor Blocks";

    public void Init() {
        _sensors = new List<IMySensorBlock>();
        ShipCore.I.GTS.GetBlocksOfType(_sensors);
    }
}
