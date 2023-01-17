using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using System;

namespace IngameScript {
    /// <summary>
    /// A single flight surface, angle is affected by primary flight inputs
    /// </summary>
    public class Surface {
        public PID PID;
    
        float _maxAngle;
        float _defaultAngle;
        float _angle;
        IMyMotorStator _rotor;
        MyIni _ini;

        public float AngleRad {
            get { return _angle; }
            set { _angle = Math.Min(MaxAngleRad, Math.Max(value, -MaxAngleRad)); }
        }
        const float DEG2RAD = (float)Math.PI / 180, RAD2DEG = 180 / (float)Math.PI;
        public float AngleDeg {
            set { AngleRad = value * DEG2RAD; }
            get { return AngleRad * RAD2DEG; }
        }

        public float MaxAngleRad {
            get { return _maxAngle; }
            set {
                _maxAngle = value;
                _rotor.UpperLimitRad = _maxAngle;
                _rotor.LowerLimitRad = -_maxAngle;
            }
        }

        public float MaxAngleDeg {
            set { MaxAngleRad = value * DEG2RAD; }
            get { return MaxAngleRad * RAD2DEG; }
        }

        public Surface(IMyMotorStator motor) {
            _rotor = motor;
            if(!_ini.TryParse(_rotor.CustomData)) {
                Log.Panic($"Failed to parse custom data for control surface {_rotor.DisplayNameText}");
            }

            MaxAngleDeg = 22;
            AngleRad = 0F;
        }

        public void Update() {
            _rotor.TargetVelocityRPM = PID.Step(AngleRad - _rotor.Angle);
        }
    }

}
