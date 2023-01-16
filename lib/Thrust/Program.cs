using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;
using System.Collections.Immutable;

namespace IngameScript {
    /// <summary>
    /// General purpose thrust controller that manipulates thruster override to 
    /// set a ship's velocity vector
    /// </summary>
    public class Thrust {
        public Vector3 VelLocal;
        public Vector3 VelGlobal {
            get { return Vector3.TransformNormal(VelLocal, Ref.WorldMatrix); }
            set { VelLocal = Vector3.TransformNormal(value, MatrixD.Transpose(Ref.WorldMatrix)); }
        }

        public IMyShipController Ref;
        public float Rate = 0.7F;
        float _mass;
        List<IMyThrust> _right, _left, _up, _down, _fw, _bw;
        
        public Thrust(IMyGridTerminalSystem GTS, IMyShipController _ref) {
            Ref = _ref;
            Func<Base6Directions.Direction, List<IMyThrust>> fill = (dir) => {
                var list = new List<IMyThrust>();
                GTS.GetBlocksOfType(list, thrust => thrust.Orientation.Forward == dir);
                return list;
            };

            _fw = fill(Base6Directions.Direction.Forward);
            _bw = fill(Base6Directions.Direction.Backward);
            _right = fill(Base6Directions.Direction.Right);
            _left = fill(Base6Directions.Direction.Left);
            _up = fill(Base6Directions.Direction.Up);
            _down = fill(Base6Directions.Direction.Down);
        }
        
        public void UpdateMass() => _mass = Ref.CalculateShipMass().TotalMass;

        public void Step() {
            var force = (VelLocal - Ref.GetShipVelocities().LinearVelocity) / _mass * Rate;
            ApplyAccel(_right, force.Y); 
            ApplyAccel(_left, -force.Y);
            ApplyAccel(_up, force.Z);
            ApplyAccel(_down, -force.Z);
            ApplyAccel(_bw, force.X);
            ApplyAccel(_fw, -force.X);
        }

        private void ApplyAccel(List<IMyThrust> list, double accel) {
            foreach(var th in list) {
                if(accel <= 0) break;
                th.ThrustOverride = (float)accel;
                accel -= th.MaxEffectiveThrust;
            }
        }
    } 
}
