using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

namespace IngameScript {
    /// <summary>
    /// Flight controller containing all control surfaces and thrust devices on the aircraft,
    /// which can be controlled using the <c>Controls</c> property
    /// </summary>
    public class FlightController {
        public Process<Nil> Update1;
        public FlightControls Controls;

        List<Surface> _surfaces;
        
        const string
            CONTROLLER_SECT = "controller",
            COCKPIT_SECTION = "cockpit";
        
        /// <summary>
        /// Create a new <c>FlightController</c>, collecting all registered control devices
        /// by their custom data's ini sections
        /// </summary>
        public FlightController(IMyGridTerminalSystem GTS) {
            var rotors = new List<IMyMotorStator>();
            GTS.GetBlocksOfType(rotors, r => MyIni.HasSection(r.CustomData, Surface.SECTION));
            foreach(var rotor in rotors) {
                _surfaces.Add(new Surface(rotor));
            }

            Update1 = new MethodProcess<Nil>(Tick1);
        }

        public static float IniVal(MyIni ini, string sec, string name, float deflt = 0F) {
            if(ini.ContainsKey(sec, name)) {
                float val;
                if(!ini.Get(sec, name).TryGetSingle(out val)) {
                    Log.Panic($"Failed to parse ini value {sec}.{name}");
                }

                return val;
            } else {
                return deflt;
            }
        }

        private IEnumerator<Nil> Tick1() {
            for(;;) {
                try {
                    foreach(var surface in _surfaces) {
                        surface.FeedInput(Controls.Primary);
                        yield return Nil._;
                    }
                } finally {

                }
            } 
        }
    }
    
    /// <summary>
    /// A collection of flight input terms, both directly user-controlled like pitch, roll, and yaw,
    /// and flight controller-controlled terms like flaps and wing sweep
    /// </summary>
    public struct FlightControls {
        public Vector3 Primary;
        public float Throttle;
    }
}
