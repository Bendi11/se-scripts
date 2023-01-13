using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace IngameScript {
    partial class Program: MyGridProgram {
        public class OrientationSystem: IMySystem {
            public readonly GyroController gyro;
            public bool StopOnOriented { get; set; } = true;

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
