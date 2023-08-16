using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;
using VRageMath;
using System;

/// <summary>
/// Autopilot using PIDs to control each axis of the velocity vector
/// </summary>
public class Autopilot {
    public Vector3D PositionWorld;
    public Vector3D PositionLocal {
        get { return Vector3D.TransformNormal(PositionWorld - Ref.GetPosition(), MatrixD.Transpose(Ref.WorldMatrix)); }
        set { PositionWorld = Vector3D.Transform(value, Ref.WorldMatrix); }
    }

    public IMyCubeBlock Ref;
    public double SpeedLimit = 10;
    PID _x, _y, _z;

    Thrust _thrust;

    public bool Enabled {
        get { return _thrust.Enabled; }
        set { _thrust.Enabled = value; }
    }

    public Autopilot(IMyGridTerminalSystem GTS, IMyShipController _ref) {
        _thrust = new Thrust(GTS, _ref);
        Ref = _ref;
        _x = new PID(0.2, 0, 0);
        _y = new PID(0.2, 0, 0);
        _z = new PID(0.2, 0, 0);
    }

    public void UpdateMass() => _thrust.UpdateMass();

    public void Step() {
        Vector3D error = PositionWorld - Ref.GetPosition();
        Func<double, double> clamp = (val) => Math.Min(SpeedLimit, Math.Max(val, -SpeedLimit));
        Vector3D control = new Vector3D(
            clamp(_x.Step(error.X)),
            clamp(_y.Step(error.Y)),
            clamp(_z.Step(error.Z))
        );
        _thrust.VelWorld = control;
        _thrust.Step();
    }
}

class PID {
    public double P, I, D;
    double _ts, _tsInv, _errsum, _lasterr;
    bool first = true;

    public PID(double p, double i, double d, double ts = 0.16) {
        P = p;
        I = i;
        D = d;
        _ts = ts;
        _tsInv = _ts / 1;
        _errsum = 0;
    }

    public double Step(double err) {
        double tD = (err - _lasterr) * _tsInv;
        if(first) {
            tD = 0;
            first = false;
        }

        _errsum = _errsum + err * _ts;
        _lasterr = err;
        return P * err + I * _errsum + D * tD;
    }
}
