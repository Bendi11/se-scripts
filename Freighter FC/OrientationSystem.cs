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
        public class OrientationSystem: IMySystem<float> {
            List<PIDGyro> gyros = new List<PIDGyro>();
            IMyShipController cockpit;
            public Vector3 target { get; set; }
            private float angle = 0F;
            public bool Oriented {
                get {
                    return Math.Abs(angle) < 0.5;
                }
            }

            public OrientationSystem(IMyGridTerminalSystem gridTerminalSystem) {
                List<IMyGyro> gyro = new List<IMyGyro>();
                gridTerminalSystem.GetBlocksOfType(gyro);
                foreach(var g in gyro) {
                    g.GyroOverride = true;
                    gyros.Add(new PIDGyro(g, new PID(0F, 0.5F, 0F, 0.5F)));
                }

                cockpit = gridTerminalSystem.GetBlockWithName("COCKPIT") as IMyShipController;
            }

            public override void Begin() { Progress = Run(); }
            
            private IEnumerator<float> Run() {
                Matrix gor;
                cockpit.Orientation.GetMatrix(out gor);
                var front = gor.Backward;
                angle = 10F; 
                while(!Oriented) {
                    angle = AngleBetween(cockpit.WorldMatrix.GetOrientation().Forward, target);

                    foreach(var g_pid in gyros) {
                        var gyro = g_pid.gyro;
                        gyro.Orientation.GetMatrix(out gor);
                        var localfw = Vector3.TransformNormal(front, Matrix.Transpose(gor));
                        var localmove = Vector3.TransformNormal(target, MatrixD.Transpose(gyro.WorldMatrix));

                        var axis = Vector3.Cross(localfw, localmove);
                        axis.Normalize();

                        g_pid.Update(axis);
                    }

                    yield return angle;
                }
            }

            private float AngleBetween(Vector3 a, Vector3 b) {
                return (float)Math.Acos(a.Dot(b));
            }
        }
    }
}
