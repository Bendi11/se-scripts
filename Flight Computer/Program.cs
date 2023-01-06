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
    partial class Program: MyGridProgram {
        IMyAirtightDoorBase cabin_door;
        IMySensorBlock cabin_sensor;
        IMyCockpit cockpit;
        MyTuple<IMyCockpit, IMyCockpit> benches;

        IMyTextSurface computer_screen;
        
        int log_message_count = 0;
        int num_crew_in_cabin = 0;

        List<MyDetectedEntityInfo> detected = new List<MyDetectedEntityInfo>();
        
        PreFlightChecklist checklist;
        IEnumerator<PreFlightChecklist.ShouldRender> checklist_sm = null;

        public Program() {
            Runtime.UpdateFrequency |= UpdateFrequency.Update100;
            cabin_door = GridTerminalSystem.GetBlockWithName("CABIN DOOR") as IMyAirtightDoorBase; 
            cabin_sensor = GridTerminalSystem.GetBlockWithName("CABIN SENSOR") as IMySensorBlock;
            cockpit = GridTerminalSystem.GetBlockWithName("COCKPIT") as IMyCockpit;
            benches.Item1 = GridTerminalSystem.GetBlockWithName("Bench 1") as IMyCockpit;
            benches.Item2 = GridTerminalSystem.GetBlockWithName("Bench 2") as IMyCockpit;

            checklist = new PreFlightChecklist(cockpit, GridTerminalSystem);

            computer_screen = Me.GetSurface(0);
            computer_screen.BackgroundColor = Color.Black;
            computer_screen.ContentType = ContentType.TEXT_AND_IMAGE;
            computer_screen.Font = "Monospace";
            computer_screen.FontColor = Color.Lime;
            computer_screen.FontSize = 1.3F;
        }

        public void Save() {
            
        }

        public void Main(string argument, UpdateType updateSource) {
            if(updateSource.HasFlag(UpdateType.Once)) {
                if(checklist_sm != null) {
                    if(checklist_sm.MoveNext()) {
                        if(checklist_sm.Current == PreFlightChecklist.ShouldRender.Render) { checklist.RenderScreen(); }
                        Runtime.UpdateFrequency |= UpdateFrequency.Once;
                    } else {
                        checklist_sm.Dispose();
                        checklist_sm = null;
                        checklist.Reset();
                    }
                }

                return;
            }

            if(updateSource.HasFlag(UpdateType.Script) || updateSource.HasFlag(UpdateType.Trigger) || updateSource.HasFlag(UpdateType.Terminal)) {
                Dispatch(argument);
            }

             

            if((updateSource & UpdateType.Update100) == UpdateType.Update100) {
                detected.Clear();
                num_crew_in_cabin = 0;
                cabin_sensor.DetectedEntities(detected);
                foreach(var entity in detected) {
                    if(entity.Relationship != MyRelationsBetweenPlayerAndBlock.Enemies) {
                        num_crew_in_cabin += 1;
                    }
                }

                //if(cockpit.IsUnderControl) { num_crew_in_cabin += 1; }
                if(benches.Item1.IsUnderControl) { num_crew_in_cabin += 1; }
                if(benches.Item2.IsUnderControl) { num_crew_in_cabin += 1; }

                Log("" + num_crew_in_cabin + " Soul" + (num_crew_in_cabin != 1 ? "s" : "") +" Aboard");
            }
        }

        public void Dispatch(string arg) {
            switch(arg) {
                case "CABIN_DOOR_TOGGLE": CabinDoorToggle(); break;
                case "PREFLIGHT_TOGGLE": if(checklist_sm == null) { checklist_sm = checklist.Run(); Runtime.UpdateFrequency |= UpdateFrequency.Once; } else { checklist_sm.Dispose(); checklist_sm = null; } break;
                default: Log("Unknown Command " + arg); break;
            }
        }

        private void CabinDoorToggle() {

        }

        private void Log(string msg) {
            if(log_message_count >= 10) {
                computer_screen.WriteText("", false);
                log_message_count = 0;
            }
            computer_screen.WriteText(msg + "\n", true);
            log_message_count += 1; 
        }

        class PreFlightChecklist {
            IMyTextSurface screen;
            List<IMyThrust> hydro_thrust = new List<IMyThrust>();
            List<IMyGasTank> reserve_tanks = new List<IMyGasTank>();
            List<IMyGasTank> main_tanks = new List<IMyGasTank>();
            List<IMyGasTank> oxygen_tanks = new List<IMyGasTank>();
            IMyShipController cockpit;

            enum State {
                Begin = 0,
                EmergencyHydroTanksStockpiled = 1,
                MainHydroTanksNonStockpile = 2,
                HydroThrusterOn = 3,
                OxygenTanksNotSetToStockpile = 4,
                Undocked = 5,
                COUNT,
            }

            public enum ShouldRender {
                Render,
                Unchanged,
            }

            State state = State.Begin;

            public PreFlightChecklist(IMyShipController cockpit, IMyGridTerminalSystem gridTerminalSystem) {
                screen = gridTerminalSystem.GetBlockWithName("PREFLIGHT LCD") as IMyTextSurface;
                
                screen.Font = "Monospace";
                screen.FontSize = 2F;
                screen.ClearImagesFromSelection();
                screen.Alignment = TextAlignment.CENTER;
                screen.FontColor = Color.Lime;
                screen.BackgroundColor = Color.Black;

                gridTerminalSystem.GetBlockGroupWithName("HYDRO THRUST").GetBlocksOfType(hydro_thrust);
                gridTerminalSystem.GetBlockGroupWithName("RESERVE HYDROGEN").GetBlocksOfType(reserve_tanks);
                gridTerminalSystem.GetBlockGroupWithName("MAIN HYDROGEN").GetBlocksOfType(main_tanks);
                gridTerminalSystem.GetBlockGroupWithName("OXYGEN TANKS").GetBlocksOfType(oxygen_tanks);
                this.cockpit = cockpit;
            }
            
            public void Reset() {
                state = State.Begin;
                screen.WriteText("");
            }

            public IEnumerator<ShouldRender> Run() {
                state = State.Begin;
                
                while(state != State.COUNT) {
                    switch(state) {
                        case State.Begin: break;
                        case State.EmergencyHydroTanksStockpiled:
                            foreach(var tank in reserve_tanks) {
                                if(tank.FilledRatio < 0.95) { yield return ShouldRender.Unchanged; continue; }
                            }
                        break;
                        case State.MainHydroTanksNonStockpile:
                            foreach(var tank in main_tanks) {
                                if(tank.Stockpile) { yield return ShouldRender.Unchanged; continue; }
                            }
                        break; 
                        case State.HydroThrusterOn:
                            foreach(var thrust in hydro_thrust) {
                                if(!thrust.Enabled) { yield return ShouldRender.Unchanged; continue; }
                            }
                        break;
                        case State.OxygenTanksNotSetToStockpile:
                            foreach(var tank in oxygen_tanks) {
                                if(tank.Stockpile) { yield return ShouldRender.Unchanged; continue; }
                            }
                        break;
                        case State.Undocked: if(cockpit.HandBrake) { yield return ShouldRender.Unchanged; continue; } break;
                    }

                    state += 1;
                    yield return ShouldRender.Render;
                }
                
                state = State.Begin;
                yield break;
            }

            private char CheckSymbol(State s) {
                if(state == s) { return '»'; }
                if(state < s) { return '°'; }
                else { return 'Ø'; }
            }

            public void RenderScreen() {
                screen.WriteText(RenderText(), false);
            }

            private string RenderText() {
                var sb = new StringBuilder();
                 
                for(State i = State.EmergencyHydroTanksStockpiled; i < State.COUNT; ++i) {
                    sb.Append(CheckSymbol(i));
                    sb.AppendLine(StateDescriptionString(i));
                }

                return sb.ToString();
            }

            private string StateDescriptionString(State s) {
                switch(s) {
                    case State.Begin: return "INVALID STATE";
                    case State.EmergencyHydroTanksStockpiled: return "Reserve Hydro Stockpiled";
                    case State.MainHydroTanksNonStockpile: return "Main Hydro Not Stockpiling";
                    case State.HydroThrusterOn: return "Hydro Thrusters Active";
                    case State.OxygenTanksNotSetToStockpile: return "O2 Tanks Not Stockpiling";
                    case State.Undocked: return "Undocked";
                    default: return "INVALID STATE OUT OF RANGE";
                }
            }
        }
    }
}
