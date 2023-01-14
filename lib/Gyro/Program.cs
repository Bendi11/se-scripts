using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System;
using VRageMath;

namespace IngameScript {
    /// <summary>
    /// Gyroscrope controller that accounts for multiple gyroscopes, taking the orientation of each into account
    /// </summary>
    public class GyroController {
        public List<IMyGyro> Gyros;
        public Vector3 OrientWorld = Vector3.UnitX;
        public Vector3 OrientLocal {
            get { return Vector3.TransformNormal(OrientWorld, Matrix.Transpose(_ref.WorldMatrix)); }
            set { OrientWorld = Vector3.TransformNormal(value, _ref.WorldMatrix); }
        }
        IMyTerminalBlock _ref;
        Vector3 _localAlign;
        float _ang = 0F;
        public float Rate = 0.2F;
        public bool IsOriented { get { return Math.Abs(_ang) < 0.0001; } }
        
        /// <summary>
        /// Create a new <c>GyroController</c> from a list of controlled <c>IMyGyro</c> blocks and a reference vector
        /// </summary>
        /// <param name="rf">Local direction vector that will be oriented with the <c>Orient</c> vector</param>
        public GyroController(List<IMyGyro> gyros, IMyTerminalBlock rf) {
            Gyros = gyros;
            _ref = rf;
            Matrix or;
            _ref.Orientation.GetMatrix(out or);
            _localAlign = or.Forward;
        }
        
        /// <summary>
        /// Run a single gyroscope control step, adjusting the gyros as needed to align the reference block with the current
        /// orientation vector
        /// </summary>
        /// <returns>The angle between the reference block's forward vector and one of the gyroscopes</returns>
        public float Step() {
            Matrix gor;
            GetAngle();
            foreach(var gyro in Gyros) {
                gyro.Orientation.GetMatrix(out gor);
                var localfw = Vector3.TransformNormal(_localAlign, Matrix.Transpose(gor));
                var localmove = Vector3.TransformNormal(OrientWorld, MatrixD.Transpose(gyro.WorldMatrix));

                var axis = Vector3.Cross(localfw, localmove);
                axis.Normalize();
                axis = -axis * Math.Abs(_ang) * Rate;
                
                gyro.Pitch = axis.X;
                gyro.Yaw = axis.Y;
                gyro.Roll = axis.Z;
            }

            return _ang;
        }

        private void GetAngle() => _ang = AngleBetween(_ref.WorldMatrix.GetOrientation().Forward, OrientWorld);

        private float AngleBetween(Vector3 a, Vector3 b) {
            a.Normalize();
            b.Normalize();
            var angle = (float)Math.Acos(a.Dot(b));
            if(angle == 0) { return 0.00001F; }
            if(float.IsNaN(angle)) { return -0.00001F; }
            else { return angle; }
        }
    }
}
