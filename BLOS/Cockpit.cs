using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

public partial class ShipCore {
    
}

/// All state that is persisted for each cockpit onboard the ship
struct Cockpit {
    /// Ingame interface for the cockpit the user is sitting in
    public IMyCockpit Seat;
    /// The weapon that the user has selected
    public IWeaponDevice SelectedWeapon;
    /// Screen selected for radar operation
    public Radar RadarSurface;
    
    Vector3 _orient;
    long _trackedId;

    public const string
        SECTION = "cockpit",
        RADAR_SCREEN = "radar-surface",
        LOG_SCREEN = "log-surface";

    double _lastRadarDraw;
    
    /// Start the cockpit controller process
    public IEnumerator<PS> Process() {
        for(;;) {
            if(ShipCore.I.Time - _lastRadarDraw >= 0.25) {
                RadarSurface.Render();
                _lastRadarDraw = ShipCore.I.Time;
            }

            yield return PS.Execute;
        }
    }

    public static Cockpit ReadConfig(IMyCockpit seat) {
        seat.DampenersOverride = false;
        MyIni _ini = new MyIni();
        if(!_ini.TryParse(seat.CustomData)) 
            throw new Exception($"Bad INI for cockpit {seat.DisplayName}");

        int radarScreen = _ini.Get(SECTION, RADAR_SCREEN).ToInt32();
        int logScreen = _ini.Get(SECTION, LOG_SCREEN).ToInt32();
        Cockpit me = new Cockpit();
        me.Seat = seat;
        me.SelectedWeapon = null;
        
        me.RadarSurface = new Radar(seat.GetSurface(radarScreen));
        
        var log = seat.GetSurface(logScreen);
        if(log != null) {
            Log.Init(seat.GetSurface(logScreen));
        }
        
        return me;
    }
}
