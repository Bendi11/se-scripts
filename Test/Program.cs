using Sandbox.ModAPI.Ingame;


namespace IngameScript {
    partial class Program: MyGridProgram {
        public Program() {
            var cockpit = GridTerminalSystem.GetBlockWithName("COCKPIT") as IMyShipController;
            ShipCore.Create(
                GridTerminalSystem,
                Me,
                Runtime,
                new StandardMovementController("movement")
            );
            Runtime.UpdateFrequency |= UpdateFrequency.Update10;
        }

        public void Save() {
            
        }

        public void Main(string argument, UpdateType updateSource) {
            ShipCore.I.RunMain();
        }
    }
}
