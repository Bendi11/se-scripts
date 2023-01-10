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
        public class StopSystem: IMySystem<float> {
            List<IMyThrust> thrusters = new List<IMyThrust>();
            IMyShipController cockpit;
            PID thrust = new PID(1F, 0F, 0F);
            float mass;

            public StopSystem(IMyGridTerminalSystem gridTerminalSystem) {
                cockpit = gridTerminalSystem.GetBlockWithName("COCKPIT") as IMyShipController;
                mass = cockpit.CalculateShipMass().TotalMass;
                gridTerminalSystem.GetBlocksOfType(
                    thrusters,
                    (block) => block.WorldMatrix.GetOrientation().Forward == cockpit.WorldMatrix.GetOrientation().Backward
                );
            }

            public override void Begin() {
                Progress = Run();
            }

            private IEnumerator<float> Run() {
                var speed = 0.1F;
                while(speed > 0.1F) {
                    speed = (float)cockpit.GetShipSpeed();
                    var thrust_applied = thrust.Run(-speed) * mass / thrusters.Count;

                    foreach(var thruster in thrusters) {
                        thruster.ThrustOverride = thrust_applied;
                    }
                    yield return speed;
                }
            }
        }
    }
}
