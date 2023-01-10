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
        public class OrientationSystem: IMySystem {
            public Program parent { get; set; }
            List<IMyGyro> gyros = new List<IMyGyro>();
            IMyShipController cockpit;
            public Vector3 target { get; set; }
            private float angle = 0F;
            public bool Oriented {
                get {
                    return Math.Abs(angle) < 0.001;
                }
            }

            public OrientationSystem(IMyGridTerminalSystem gridTerminalSystem) {
                gridTerminalSystem.GetBlocksOfType(gyros);
                cockpit = gridTerminalSystem.GetBlockWithName("COCKPIT") as IMyShipController;
            }
            
            protected override IEnumerator<object> Run() {
                try {
                Matrix gor;
                cockpit.Orientation.GetMatrix(out gor);
                var front = gor.Backward;
                angle = 10F; 
                foreach(var gyro in gyros) {
                    gyro.GyroOverride = true;
                }
                while(!Oriented) {
                    angle = AngleBetween(cockpit.WorldMatrix.GetOrientation().Forward, target);
                    parent.Echo("ANGLE: " + angle);

                    foreach(var gyro in gyros) {
                        gyro.Orientation.GetMatrix(out gor);
                        var localfw = Vector3.TransformNormal(front, Matrix.Transpose(gor));
                        var localmove = Vector3.TransformNormal(target, MatrixD.Transpose(gyro.WorldMatrix));

                        var axis = Vector3.Cross(localfw, localmove);
                        axis.Normalize();
                        
                        gyro.Pitch = axis.X * angle * 0.2F;
                        gyro.Yaw = axis.Y * angle * 0.2F;
                        gyro.Roll = axis.Z * angle * 0.2F;
                    }

                    yield return null;
                }
                } finally {
                    foreach(var gyro in gyros) {
                        gyro.GyroOverride = false;
                    }
                }

                yield return null;
            }

            private float AngleBetween(Vector3 a, Vector3 b) {
                var angle = (float)Math.Acos(Vector3.Normalize(a).Dot(Vector3.Normalize(b)));
                if(angle == 0) { return 0.00001F; }
                if(float.IsNaN(angle)) { return -0.00001F; }
                else { return angle; }
            }
        }
    }
}
