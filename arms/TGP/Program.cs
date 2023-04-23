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
    partial class Program: MyGridProgram {
        TGP tgp;

        public Program() {
            Log.Init(Me.GetSurface(0));

            IMyMotorStator yaw = GridTerminalSystem.GetBlockWithName("YAW") as IMyMotorStator;
            IMyMotorStator pitch = GridTerminalSystem.GetBlockWithName("PITCH") as IMyMotorStator;

            tgp = new TGP(yaw, pitch, Me);
            tgp.TargetWorldPos = new Vector3D(53574.62, -26609.58, 12070.56);
            tgp.Periodic.Begin();

            Runtime.UpdateFrequency |= UpdateFrequency.Update1;
        }

        public void Main(string arg, UpdateType source) {
            tgp.Periodic.Poll();
        }
    }

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
        public PID PitchPID = new PID(35, 0f, -0.1f, 0.1f, 0.016f);

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

        public readonly Process<Nil> Periodic;

        enum TargetKind {
            Local,
            World,
            WorldPos,
        }

        TargetKind _targetKind = TargetKind.Local;
        
        /// Depending on `_targetKind`, can be world coordinates or local / world orientation vector
        Vector3D _target = Vector3D.Forward;

        public TGP(IMyMotorStator yaw, IMyMotorStator pitch, IMyTerminalBlock _ref) {
            Yaw = yaw;
            Pitch = pitch;
            Ref = _ref;

            Periodic = new MethodProcess(Maintain);
        }
        
        /// Maintain the desired orientation
        public IEnumerator<Nil> Maintain() {
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
                 
                Log.Put($"{el}, {az} - {_target}");
                el = Double.IsNaN(el) ? 0 : el;
                az = Double.IsNaN(az) ? 0 : az;
                Yaw.TargetVelocityRad = YawPID.Run((float)(az - (Yaw.Angle - Math.PI)));
                Pitch.TargetVelocityRad = PitchPID.Run((float)(el - (Pitch.Angle - Math.PI / 2f)));

                yield return Nil._;
            }
        }
    } 
}
