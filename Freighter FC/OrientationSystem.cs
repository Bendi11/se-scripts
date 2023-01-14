using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    partial class Program: MyGridProgram {
        public class OrientationSystem: IMySystem {
            public readonly GyroController gyro;
            public bool StopOnOriented = true;

            public OrientationSystem(IMyGridTerminalSystem gridTerminalSystem) {
                List<IMyGyro> gyros = new List<IMyGyro>();
                gridTerminalSystem.GetBlocksOfType(gyros);
                var cockpit = gridTerminalSystem.GetBlockWithName("COCKPIT");
                gyro = new GyroController(gyros, cockpit);
            }
            
            protected override IEnumerator<object> Run() {
                try {
                    while((StopOnOriented && !gyro.IsOriented) ^ !StopOnOriented) {
                        gyro.Step();
                        yield return null;
                    }
                } finally {
                    foreach(var gyro in gyro.Gyros) {
                        gyro.GyroOverride = false;
                    }
                }
            }
        }
    }
}
