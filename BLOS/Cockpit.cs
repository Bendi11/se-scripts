using System;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;

public partial class ShipCore {
    
}

/// All state that is persisted for each cockpit onboard the ship
struct Cockpit {
    /// Ingame interface for the cockpit the user is sitting in
    public IMyCockpit Seat;
    /// The weapon that the user has selected
    public IWeaponDevice SelectedWeapon;
    /// Screen selected for radar operation
    public Screen RadarSurface;
    /// Currently selected input mode
    public InputMode Input;

    public enum InputMode {
        /// User's mouse and WASD + <space><control> are being used for flight control
        FlightControls,
        /// User's mouse and WASD is being used for radar target selection
        Radar,
    }

    public const string
        SECTION = "cockpit",
        RADAR_SCREEN = "radar-surface";

    
    public static Cockpit ReadConfig(IMyCockpit seat) {
        MyIni _ini = new MyIni();
        if(!_ini.TryParse(seat.CustomData)) 
            throw new Exception($"Bad INI for cockpit {seat.DisplayName}");

        int screen = _ini.Get(SECTION, RADAR_SCREEN).ToInt32();

    }
}
