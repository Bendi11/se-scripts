using System.Collections;
using Sandbox.ModAPI.Ingame;
using VRageMath;

/// A movement controller that uses gyroscopes to orient the craft in any ordered orientation and uses thrusters for dampening
public class StandardMovementController : IMovementControllerDevice {
    public Vector3D TargetOrientWorld {
        get { return _gyro.OrientWorld; }
        set {
            _gyro.OrientWorld = value;
        }
    }
    public Vector3D TargetVelocityWorld {
        get { return _thrust.VelWorld; }
        set { _thrust.VelWorld = value; }
    }
    public Vector3D ActualOrientWorld { get { return _gyro.OrientWorld; } }

    string _id;
    Thrust _thrust;
    GyroController _gyro;

    public StandardMovementController(string id) {
        _id = id;
    }

    public IEnumerator Control() {
        try {
            _gyro.Enable();
            _thrust.Enabled = true;
            for(;;) {
                _thrust.Step();
                _gyro.Step();
                yield return 0;
            }
        } finally {
            _thrust.Enabled = false;
            _gyro.Disable();

        }
    }

    public void Init() {
        _thrust = new Thrust();
        var gyros = new System.Collections.Generic.List<IMyGyro>();
        _gyro = new GyroController(gyros);
    }

    public string Name() => nameof(StandardMovementController);
    public string ID() => _id;
}
