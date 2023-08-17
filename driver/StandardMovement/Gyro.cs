using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRageMath;

/// <summary>
/// Gyroscrope controller that accounts for multiple gyroscopes, taking the orientation of each into account
/// </summary>
public class GyroController {
    public List<IMyGyro> Gyros;
    public Vector3 OrientWorld = Vector3.UnitX;
    public Vector3 OrientLocal {
        get { return Vector3.TransformNormal(OrientWorld, Matrix.Transpose(ShipCore.I.Ref.WorldMatrix)); }
        set { OrientWorld = Vector3.TransformNormal(value, ShipCore.I.Ref.WorldMatrix); }
    }
    Vector3 _localAlign;
    float _ang = 0F;
    public PID Pid = new PID(0.2f, 0f, 0f);
    
    /// <summary>
    /// Create a new <c>GyroController</c> from a list of controlled <c>IMyGyro</c> blocks and a reference vector
    /// </summary>
    public GyroController(List<IMyGyro> gyros) {
        Gyros = gyros;
        Matrix or;
        ShipCore.I.Ref.Orientation.GetMatrix(out or);
        _localAlign = or.Forward;
        Disable();
    }
    
    /// <summary>
    /// Enable gyro override on all controlled gyroscopes
    /// </summary>
    public void Enable() { foreach(var gyro in Gyros) { gyro.GyroOverride = true; } }

    /// <summary>
    /// Disable gyro override for all controlled gyroscopes
    /// </summary>
    public void Disable() { foreach(var gyro in Gyros) { gyro.GyroOverride = false; }}
    
    /// <summary>
    /// Run a single gyroscope control step, adjusting the gyros as needed to align the reference block with the current
    /// orientation vector
    /// </summary>
    /// <returns>The angle between the reference block's forward vector and one of the gyroscopes</returns>
    public float Step() {
        Matrix gor;
        var error = GetAngle();
        var speed = Pid.Run(error);
        foreach(var gyro in Gyros) {
            gyro.Orientation.GetMatrix(out gor);
            var localfw = Vector3.TransformNormal(_localAlign, Matrix.Transpose(gor));
            var localmove = Vector3.TransformNormal(OrientWorld, MatrixD.Transpose(gyro.WorldMatrix));

            var axis = Vector3.Cross(localfw, localmove);
            axis.Normalize();
            axis = -axis * speed;

            gyro.Pitch = axis.X;
            gyro.Yaw = axis.Y;
            gyro.Roll = axis.Z;
        }

        return _ang;
    }

    private float GetAngle() => _ang = AngleBetween(ShipCore.I.Ref.WorldMatrix.GetOrientation().Forward, OrientWorld);

    private float AngleBetween(Vector3 a, Vector3 b) {
        a.Normalize();
        b.Normalize();
        var angle = (float)Math.Acos(a.Dot(b));
        if(angle == 0) { return 0.00001F; }
        if(float.IsNaN(angle)) { return -0.00001F; }
        else { return angle; }
    }
}
