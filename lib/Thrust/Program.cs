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

        public IMyShipController Control = null;
        public IMyTerminalBlock Ref;
        public float Rate = 0.7F;
        float _mass;
        List<IMyThrust> _all = new List<IMyThrust>();
        List<IMyThrust> _right, _left, _up, _down, _fw, _bw;
        
        public Thrust(IMyGridTerminalSystem GTS, IMyTerminalBlock _ref, float mass) {
            Ref = _ref;
            Func<Vector3D, List<IMyThrust>> fill = (dir) => {
                var list = new List<IMyThrust>();
                GTS.GetBlocksOfType(list, thrust => thrust.WorldMatrix.Backward == dir);
                return list;
            };
            
            GTS.GetBlocksOfType(_all);
            var mat = Ref.WorldMatrix;
            _fw = fill(mat.Forward);
            _bw = fill(mat.Backward);
            _right = fill(mat.Right);
            _left = fill(mat.Left);
            _up = fill(mat.Up);
            _down = fill(mat.Down);
            UpdateMass(mass);
        }
        
        public Thrust(IMyGridTerminalSystem GTS, IMyShipController control) : this(GTS, control as IMyTerminalBlock, control.CalculateShipMass().TotalMass) {
            this.Control = control; 
        }
        
        
        public void UpdateMass() => _mass = Control != null ? Control.CalculateShipMass().TotalMass : _mass;
        public void UpdateMass(float mass) => _mass = mass;

        public void Step() {
            if(!_enabled) return;
            var force = (VelLocal - Vector3D.TransformNormal(Ref.CubeGrid.LinearVelocity, MatrixD.Transpose(Ref.WorldMatrix))) * _mass * Rate;
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
