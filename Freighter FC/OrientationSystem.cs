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
        public class OrientationSystem {
            List<IMyGyro> gyros;
            IMyShipController cockpit;

            public OrientationSystem(IMyGridTerminalSystem gridTerminalSystem) {
                gridTerminalSystem.GetBlocksOfType(gyros);
                cockpit = gridTerminalSystem.GetBlockGroupWithName("COCKPIT") as IMyShipController;
                foreach(var gyro in gyros) {
                    gyro.GyroOverride = true;
                }
            }
            
            public void Run() {
                var fw = -Vector3.Normalize(cockpit.GetShipVelocities().LinearVelocity);
                Matrix gor;
                cockpit.Orientation.GetMatrix(out gor);
                var front = gor.Forward;

                foreach(var gyro in gyros) {
                    gyro.Orientation.GetMatrix(out gor);
                    var localfw = Vector3.Transform(front, Matrix.Transpose(gor));
                    var localmove = Vector3.Transform(fw, MatrixD.Transpose(gyro.WorldMatrix.GetOrientation()));

                    var axis = Vector3.Cross(localfw, localmove);
                    axis.Normalize();

                    gyro.Pitch = axis.X;
                    gyro.Yaw = axis.Y;
                    gyro.Roll = axis.Z;
                }
            }
        }
    }
}
