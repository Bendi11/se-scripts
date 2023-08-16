using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRageMath;


/// <summary>
/// General purpose thrust controller that manipulates thruster override to 
/// set a ship's velocity vector
/// </summary>
public class Thrust {
    public Vector3 VelLocal;
    public Vector3 VelWorld {
        get { return Vector3.TransformNormal(VelLocal, Ref.WorldMatrix); }
        set { VelLocal = Vector3.TransformNormal(value, MatrixD.Transpose(Ref.WorldMatrix)); }
    }
    
    bool _enabled = false;
    public bool Enabled {
        get { return _enabled; }
        set {
            _enabled = value;
            if(!value) {
                foreach(var th in _all) th.ThrustOverride = 0;
            }
        }
    }

    public IMyTerminalBlock Ref;
    public float Rate = 0.7F;
    float _mass;
    List<IMyThrust> _all = new List<IMyThrust>();
    List<IMyThrust> _right, _left, _up, _down, _fw, _bw;
    
    public Thrust() {
        Func<Vector3D, List<IMyThrust>> fill = (dir) => {
            var list = new List<IMyThrust>();
            ShipCore.I.GTS.GetBlocksOfType(list, thrust => thrust.WorldMatrix.Backward == dir);
            return list;
        };
        
        ShipCore.I.GTS.GetBlocksOfType(_all);
        var mat = Ref.WorldMatrix;
        _fw = fill(mat.Forward);
        _bw = fill(mat.Backward);
        _right = fill(mat.Right);
        _left = fill(mat.Left);
        _up = fill(mat.Up);
        _down = fill(mat.Down);
    }
    
    public void Step() {
        if(!_enabled) return;
        var force = (VelLocal - Vector3D.TransformNormal(Ref.CubeGrid.LinearVelocity, MatrixD.Transpose(Ref.WorldMatrix))) * ShipCore.I.ShipMass * Rate;
        ApplyAccel(_right, force.X); 
        ApplyAccel(_left, -force.X);
        ApplyAccel(_up, force.Y);
        ApplyAccel(_down, -force.Y);
        ApplyAccel(_bw, force.Z);
        ApplyAccel(_fw, -force.Z);
    }

    private void ApplyAccel(List<IMyThrust> list, double accel) {
        foreach(var th in list) {
            th.ThrustOverride = Math.Max((float)accel, 0.001F);
            accel -= th.MaxEffectiveThrust;
        }
    }
}
