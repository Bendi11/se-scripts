using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using System.Collections.Generic;

namespace IngameScript {
    /// <summary>
    /// A flight computer that utilizes a <c>FlightController</c> to control an aircraft,
    /// providing secondary control devices like autopilot and trim
    /// </summary>
    public class FlightComputer {
        public readonly FlightController Controller;
        IMyShipController _cockpit;
        LcdWriter _lcd;

        const string
            SECTION = "flightcomputer",
            LCDNO = "screenno";

        public FlightComputer(IMyGridTerminalSystem GTS) {
            MyIni ini = new MyIni();
            Controller = new FlightController(GTS);

            var cockpits = new List<IMyShipController>();
            GTS.GetBlocksOfType(cockpits, c => MyIni.HasSection(c.CustomData, SECTION));
            if(cockpits.Count != 1) {
                Log.Panic("Detected 0 or more than 1 cockpit(s) aboard craft");
            }
            _cockpit = cockpits[0];

            if(!ini.TryParse(_cockpit.CustomData)) {
                Log.Panic("Failed to parse flight computer section in cockpit");
            }
            
            if(_cockpit is IMyTextSurfaceProvider) {
                int screenNo = ini.Get(SECTION, LCDNO).ToInt32();
                if(screenNo < 0 || screenNo >= ((IMyTextSurfaceProvider)_cockpit).SurfaceCount) {
                    Log.Panic($"Invalid flightcomputer.screenno {screenNo}");
                }
                _lcd = new LcdWriter(((IMyTextSurfaceProvider)_cockpit).GetSurface(screenNo));
            }
        }
    } 
}
