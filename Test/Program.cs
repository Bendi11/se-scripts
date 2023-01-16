using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace IngameScript {
    partial class Program: MyGridProgram {
        Thrust _au;
         
        public Program() {
            var cockpit = GridTerminalSystem.GetBlockWithName("COCKPIT") as IMyShipController;
            _au = new Thrust(GridTerminalSystem, cockpit) { prog = this };
            _au.Enabled = false;
            _au.VelLocal = Vector3.Zero;
            //_au.PositionWorld = cockpit.GetPosition();
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
