using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace IngameScript {
    partial class Program: MyGridProgram {
        Autopilot _au;
         
        public Program() {
            var cockpit = GridTerminalSystem.GetBlockWithName("COCKPIT") as IMyShipController;
            _au = new Autopilot(GridTerminalSystem, cockpit);
            _au.Enabled = false;
            _au.PositionWorld = cockpit.GetPosition() + Vector3D.Up * 10;
            Runtime.UpdateFrequency |= UpdateFrequency.Update10;
        }

        public void Save() {
            
        }

        public void Main(string argument, UpdateType updateSource) {
            if(!updateSource.HasFlag(UpdateType.Update10)) {
                _au.Enabled = !_au.Enabled;
            }
            _au.Step();
        }
    }
}
