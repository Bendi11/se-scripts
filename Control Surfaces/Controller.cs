using Sandbox.ModAPI.Ingame;
using System.Collections.Immutable;

namespace IngameScript {
    /// <summary>
    /// Flight controller containing all control surfaces and thrust devices on the aircraft
    /// </summary>
    public class FlightController {
        public Process PeriodicProcess;
        IMyShipController _cockpit;
        ImmutableList<IFlightDevice> _devices;
        
        /// <summary>
        /// A single flight control device whose output is determined by
        /// terms in a <c>FlightInput</c>
        /// </summary>
        public interface IFlightDevice {
            /// <summary>
            /// Update this device's output based on the given control inputs
            /// </summary>
            void Control(ref FlightInput input);
        }

        public const string
            SURFACE_SECT = "surface",
            CONTROLLER_SECT = "controller";
        
        /// <summary>
        /// Create a new <c>FlightController</c>, collecting all registered control devices
        /// by their custom data's ini sections
        /// </summary>
        public FlightController(IMyGridTerminalSystem GTS) {
            
        }
    }
    
    /// <summary>
    /// A collection of flight input terms, both directly user-controlled like pitch, roll, and yaw,
    /// and flight controller-controlled terms like flaps and wing sweep
    /// </summary>
    public struct FlightInput {
        public float Pitch, Yaw, Roll, Flap, WingSweep;
    }
}
