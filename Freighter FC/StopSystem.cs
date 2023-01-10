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
        public class StopSystem: IMySystem {
            public Program parent { get; set; }
            List<IMyThrust> thrusters = new List<IMyThrust>();
            IMyShipController cockpit;
            PID thrust = new PID(0.07F, 0F, 0F);
            float mass;

            public StopSystem(IMyGridTerminalSystem gridTerminalSystem) {
                cockpit = gridTerminalSystem.GetBlockWithName("COCKPIT") as IMyShipController;
                mass = cockpit.CalculateShipMass().TotalMass;
                gridTerminalSystem.GetBlocksOfType(
                    thrusters,
                    (block) => block.WorldMatrix.GetOrientation().Forward == cockpit.WorldMatrix.GetOrientation().Backward
                );
            }

            protected override IEnumerator<object> Run() {
                try {
                    var speed = 0.2F;
                    Matrix or;
                    cockpit.Orientation.GetMatrix(out or);

                    while(speed > 0.02F) {
                        var transformed = Vector3D.TransformNormal(cockpit.GetShipVelocities().LinearVelocity, MatrixD.Transpose(cockpit.WorldMatrix));
                        speed = (float)transformed.Z;
                        var thrust_applied = -thrust.Run(-speed);

                        foreach(var thruster in thrusters) {
                            thruster.ThrustOverridePercentage = thrust_applied;
                        }
                        yield return null;
                    }
                } finally {
                    foreach(var thruster in thrusters) { thruster.ThrustOverride = 0F; }
                }

                yield return null;
            }
        }
    }
}
