using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace IngameScript {
    partial class Program: MyGridProgram {
        Sendy<int> i;
        public Program() {
            var cockpit = GridTerminalSystem.GetBlockWithName("COCKPIT") as IMyShipController;
            Runtime.UpdateFrequency |= UpdateFrequency.Update10;
        }

        public void Save() {
            
        }

        public void Main(string argument, UpdateType updateSource) {
            i.SendRequest(2, 3);
            Log.Put("TEST");
            if(!updateSource.HasFlag(UpdateType.Update10)) {
            }
        }
    }
}
