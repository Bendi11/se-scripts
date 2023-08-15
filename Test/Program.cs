using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace IngameScript {
    partial class Program: MyGridProgram {
        public Program() {
            var cockpit = GridTerminalSystem.GetBlockWithName("COCKPIT") as IMyShipController;
            ShipCore.Create();
            Runtime.UpdateFrequency |= UpdateFrequency.Update10;
        }

        public void Save() {
            
        }

        public void Main(string argument, UpdateType updateSource) {
            if(!updateSource.HasFlag(UpdateType.Update10)) {
            
            }
        }
    }
}
