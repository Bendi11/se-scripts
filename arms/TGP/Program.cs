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
    /// General controller for a pitch + yaw gimbal
    public class TGP {
        /// Device used for yaw angle control
        public IMyMotorStator Yaw;
        /// Device used for pitch angle control
        public IMyMotorStator Pitch;
        /// Reference block used to convert world coordinates to local 
        public IMyTerminalBlock Ref;
    
        /// PID tune used to control the yaw axis
        public PID YawPID = new PID(35, 0f, -0.1f);
        /// PID tune used to control the pitch axis
        public PID PitchPID = new PID(35, 0f, -0.1f, 0.1f);

        public Vector3D TargetLocal {
            set {
                _targetKind = TargetKind.Local;
                _target = value;
            }
        }

        public Vector3D TargetWorld {
            set {
                _targetKind = TargetKind.World;
                _target = value;
            }
        }

        public Vector3D TargetWorldPos {
            set {
                _targetKind = TargetKind.WorldPos;
                _target = value;
            }
        }

        enum TargetKind {
            Local,
            World,
            WorldPos,
        }

        float _yawHome = 0f;
        float _pitchHome = 0f;

        TargetKind _targetKind = TargetKind.Local;
        
        /// Depending on `_targetKind`, can be world coordinates or local / world orientation vector
        Vector3D _target = Vector3D.Forward;

        public TGP(IMyMotorStator yaw, IMyMotorStator pitch, IMyTerminalBlock _ref) {
            Yaw = yaw;
            Pitch = pitch;
            
            MyIni ini = new MyIni();
            MyIniParseResult res;

            if(!ini.TryParse(Yaw.CustomData, out res)) {
                Log.Warn($"Failed to parse custom data for yaw: {res.ToString()}");
            }

            if(ini.ContainsKey("tgp", "center")) {
                _yawHome = (float)ini.Get("tgp", "center").ToDouble();
                _yawHome *= (float)Math.PI / 180f;
            }
            
            ini.Clear();
            if(!ini.TryParse(Pitch.CustomData, out res)) {
                Log.Warn($"Failed to parse custom data for pitch: {res.ToString()}");
            }

            if(ini.ContainsKey("tgp", "center")) {
                _pitchHome = (float)ini.Get("tgp", "center").ToDouble();
                _pitchHome *= (float)Math.PI / 180f;
            }
            Ref = _ref;
        }
        
        /// Maintain the desired orientation
        public IEnumerable<Nil> Maintain() {
            double az = 0;
            double el = 0;
            for(;;) {
                Vector3D localTarget = _target;

                switch(_targetKind) {
                    case TargetKind.World: {
                        localTarget = Vector3D.TransformNormal(_target, MatrixD.Transpose(Ref.WorldMatrix));
                    } break;

                    case TargetKind.WorldPos: {
                        localTarget = Vector3D.TransformNormal(
                            (_target - Ref.WorldMatrix.Translation).Normalized(),
                            MatrixD.Transpose(Ref.WorldMatrix)
                        );
                    } break;
                }

                Vector3D.GetAzimuthAndElevation(
                    localTarget,
                    out az,
                    out el
                );

                el = Double.IsNaN(el) ? 0 : el;
                az = Double.IsNaN(az) ? 0 : az;
                Yaw.TargetVelocityRad = YawPID.Run((float)(az - (Yaw.Angle - _yawHome)));
                Pitch.TargetVelocityRad = PitchPID.Run((float)(el - (Pitch.Angle - _pitchHome)));
                yield return Nil._;
            }
        }
    } 
}
