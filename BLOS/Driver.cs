using System.Collections;
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

/// A specialization of an IDevice that provides ship movement and orientation control to the flight controller
public interface IMovementControllerDevice : IDevice {
    /// Get a process that will constantly update output devices to maintain orientation and velocity,
    /// and **must** disable all controls when the task is cancelled
    IEnumerator Control();
    /// The orientation in world frame that the flight controller requests
    Vector3D TargetOrientWorld { get; set; }
    /// The velocity in world frame that the flight controller requests
    Vector3D TargetVelocityWorld { get; set; }
    /// Set by the movement controller to indicate that its targeted orientation is
    /// different from the ordered orientation, for movement controllers that must
    /// orient themselves to achieve the ordered velocity (eg. Single thruster craft)
    Vector3D ActualOrientWorld { get; }
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
    IEnumerable ScanProcess();
    /// Set to a value between 0 and 1 to indicate the desired scan radius for sensors that
    /// 'sweep'
    float ScanSize { get; set; }
    /// A vector that can indicate the position of an SPI or the direction of an SPI
    Vector3D VSPI { get; set; }
    /// An entity ID to a tracked contact that can be used for STT
    long CSPI { get; set; }
}
