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
    static class Const {
        public const string SURFACE_INI_SECTION = "controlsurface";
    }

    partial class Program: MyGridProgram {
        MyIni _ini;
        List<Surface> _surfaces = new List<Surface>();
        ControlInput _input = new ControlInput();

        public Program() {
            try {
                Log.Init(Me.GetSurface(0));
                var rotors = new List<IMyMotorStator>();
                GridTerminalSystem.GetBlocksOfType(rotors, r => MyIni.HasSection(r.CustomData, Const.SURFACE_INI_SECTION));
                foreach(var rotor in rotors) {
                    _surfaces.Add(new Surface(rotor));
                }
            } catch(Exception e) {
                Log.Panic(e.ToString()); 
            }
        }

        public void Save() {
            
        }

        public void Main(string argument, UpdateType updateSource) {
             
        }
    }
    
    /// <summary>
    /// All user-controlled (pitch, yaw, roll) and computer controlled (flaps, wing sweep) user inputs
    /// </summary>
    struct ControlInput {
    
    }
}
