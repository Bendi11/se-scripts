using Sandbox.ModAPI.Ingame;
using VRageMath;

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
