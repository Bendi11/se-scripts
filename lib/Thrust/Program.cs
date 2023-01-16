using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System;
using VRageMath;

namespace IngameScript {
    /// <summary>
    /// General purpose thrust controller that manipulates thruster override to 
    /// set a ship's velocity vector
    /// </summary>
    public class Thrust {
        public MyGridProgram prog;
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
                } else {
                    UpdateMass();
                }
            }
        }

        public IMyShipController Ref;
        public float Rate = 0.7F;
        float _mass;
        List<IMyThrust> _all = new List<IMyThrust>();
        List<IMyThrust> _right, _left, _up, _down, _fw, _bw;
        
        public Thrust(IMyGridTerminalSystem GTS, IMyShipController _ref) {
            Ref = _ref;
            Func<Base6Directions.Direction, List<IMyThrust>> fill = (dir) => {
                var list = new List<IMyThrust>();
                GTS.GetBlocksOfType(list, thrust => thrust.Orientation.Forward == dir);
                return list;
            };
            
            GTS.GetBlocksOfType(_all);
            _fw = fill(Base6Directions.Direction.Backward);
            _bw = fill(Base6Directions.Direction.Forward);
            _right = fill(Base6Directions.Direction.Left);
            _left = fill(Base6Directions.Direction.Right);
            _up = fill(Base6Directions.Direction.Down);
            _down = fill(Base6Directions.Direction.Up);
            UpdateMass();
        }
        
        public void UpdateMass() => _mass = Ref.CalculateShipMass().TotalMass;

        public void Step() {
            if(!_enabled) return;
            var force = (VelLocal - Vector3D.TransformNormal(Ref.GetShipVelocities().LinearVelocity, MatrixD.Transpose(Ref.WorldMatrix))) * _mass * Rate;
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
}
