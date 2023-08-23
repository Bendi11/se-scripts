using Sandbox.ModAPI.Ingame;


namespace IngameScript {
    partial class Program: MyGridProgram {
        public Program() {
            ShipCore.Create(
                GridTerminalSystem,
                Me,
                Runtime,
                new SensorBlockDevice()
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
