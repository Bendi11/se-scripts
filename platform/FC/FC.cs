using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;
using System.Collections.Immutable;

namespace IngameScript {
    public class FlightComputer {
        IMyCockpit cockpit;
        TGP tgp = null;

        public FlightComputer(MyGridProgram program) {
            List<IMyCockpit> cockpits = new List<IMyCockpit>();
            program
                .GridTerminalSystem
                .GetBlocksOfType(cockpits, b => b.IsSameConstructAs(program.Me) && MyIni.HasSection(b.CustomData, "fc-cockpit"));

            if(cockpits.Count != 1) { Log.Panic($"Found {cockpits.Count} cockpits with a [fc-cockpit] INI tag, expected 1"); }

            cockpit = cockpits.First();

            
            if(tgpGroup != null) {

                tgpGroup.GetBlocksOfType()
            }

        }

        public IEnumerable<Nil> RunLoop() {
            for(;;) {
                cockpit.RotationIndicator;
            }
        }
    }
}
