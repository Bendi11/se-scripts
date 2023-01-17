using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using System;
using VRageMath;

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

        Vector3 _sensitivity;

        public const string
            SECTION = "surface",
            DEFAULT_ANGLE = "default-deg",
            MAX_ANGLE = "lim-deg",

            PITCH = "pitch",
            YAW = "yaw",
            ROLL = "roll";

        public float AngleRad {
            get { return _angle; }
            set { _angle = Math.Min(MaxAngleRad, Math.Max(value + _defaultAngle, -MaxAngleRad)); }
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
                _rotor.UpperLimitRad = _defaultAngle + _maxAngle;
                _rotor.LowerLimitRad = _defaultAngle - _maxAngle;
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

            _defaultAngle = FlightController.IniVal(_ini, SECTION, DEFAULT_ANGLE, 0) * DEG2RAD;
            MaxAngleDeg = FlightController.IniVal(_ini, SECTION, MAX_ANGLE, 22);
            _sensitivity.X = FlightController.IniVal(_ini, SECTION, PITCH);
            _sensitivity.Y = FlightController.IniVal(_ini, SECTION, YAW);
            _sensitivity.Z = FlightController.IniVal(_ini, SECTION, ROLL);
            AngleRad = _defaultAngle;
        }
        
        /// <summary>
        /// Feed a primary control vector of (pitch, yaw, roll) into the angle of this rotor
        public void FeedInput(Vector3 primaryControl) {
            float ratio = (primaryControl * _sensitivity).Sum;
            AngleRad = _maxAngle * ratio;
        }

        public void Update() {
            _rotor.TargetVelocityRPM = PID.Step(AngleRad - _rotor.Angle);
        }
    }

}
