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
        class A {}
        IMyDoor cabin_door;
        IMySensorBlock cabin_sensor;
        List<IMyAirVent> vents = new List<IMyAirVent>();
        IMyCockpit cockpit;
        MyTuple<IMyCockpit, IMyCockpit> benches;
        List<IMyGasTank> oxygen_tanks = new List<IMyGasTank>();
        IMyReflectorLight decom_light;

        List<IMySoundBlock> sound_blocks = new List<IMySoundBlock>();
        string sound_b4_alarm = null;
        bool playing_b4_alarm = false;

        List<MyDetectedEntityInfo> detected = new List<MyDetectedEntityInfo>();
        
        PreFlightChecklist checklist;
        IEnumerator<PreFlightChecklist.ShouldRender> checklist_sm = null;

        Dictionary<string, Action> commands = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);
        MyCommandLine cmdline = new MyCommandLine();

        Action onDoorStateChange = null;
        Action onVentDepressurize = null;

        int decom_countdown = -1;

        public static T nnull<T>(T me, string msg = null) {
            msg = msg ?? typeof(T).ToString() + " is null";
            if(me == null) {
                Log.Error(msg);
                throw new Exception(msg);
            } 
            return me;
        }
        public static List<T> nnull<T>(List<T> list, string msg = null) {
            msg = msg ?? typeof(List<T>).ToString() + " is empty";
            if(list.Count == 0) {
                Log.Error(msg);
                throw new Exception(msg);
            }
            return list;
        }

        public Program() {
            Log.Init(Me.GetSurface(0));
            Runtime.UpdateFrequency |= UpdateFrequency.Update100 | UpdateFrequency.Update10;
            cabin_door = nnull(GridTerminalSystem.GetBlockWithName("CABIN DOOR") as IMyDoor);
            cabin_sensor = nnull(GridTerminalSystem.GetBlockWithName("CABIN SENSOR") as IMySensorBlock);
            cockpit = nnull(GridTerminalSystem.GetBlockWithName("COCKPIT") as IMyCockpit);
            benches.Item1 = nnull(GridTerminalSystem.GetBlockWithName("Bench 1") as IMyCockpit);
            benches.Item2 = nnull(GridTerminalSystem.GetBlockWithName("Bench 2") as IMyCockpit);
            decom_light = nnull(GridTerminalSystem.GetBlockWithName("CABIN DECOM LIGHT") as IMyReflectorLight);
            GridTerminalSystem.GetBlockGroupWithName("O2 VENTS").GetBlocksOfType(vents);
            GridTerminalSystem.GetBlockGroupWithName("OXYGEN TANKS").GetBlocksOfType(oxygen_tanks);
            GridTerminalSystem.GetBlocksOfType(sound_blocks);
            nnull(vents);

            decom_light.Radius = 13.3F;
            decom_light.Intensity = 10F;
            decom_light.Color = Color.Red;
            decom_light.BlinkLength = 50F;
            decom_light.Enabled = false;
            
            checklist = new PreFlightChecklist(cockpit, GridTerminalSystem, nnull(cockpit.GetSurface(0)));

            commands.Add("cabindoor", CabinDoorToggle);
            commands.Add("preflight", Preflight);
            CabinDoorClose();
        }

        public void Save() {
            
        }

        public void Main(string argument, UpdateType updateSource) {
            if(updateSource.HasFlag(UpdateType.Update10)) {
                if(checklist_sm != null) {
                    if(checklist_sm.MoveNext()) {
                        if(checklist_sm.Current != PreFlightChecklist.ShouldRender.Unchanged) { checklist.RenderScreen(); }
                    } else {
                        checklist_sm.Dispose();
                        checklist_sm = null;
                        checklist.Reset();
                    }
                }

                if(onDoorStateChange != null && cabin_door.Status != DoorStatus.Closing && cabin_door.Status != DoorStatus.Opening) {
                    onDoorStateChange();
                    onDoorStateChange = null;
                }

                if(onVentDepressurize != null) {
                    bool o2_full = true;
                    foreach(var tank in oxygen_tanks) { if(tank.FilledRatio < 0.999) { o2_full = false; break; } }
                    bool depres = true;
                    foreach(var vent in vents) { if(vent.GetOxygenLevel() > 0.001) { depres = false; break; }}

                    if(o2_full || depres) {
                        if(o2_full) {
                            Log.Put("O2 tank max: venting air");
                        }
                        onVentDepressurize();
                        onVentDepressurize = null;
                    }
                }

                return;
            }

            if(updateSource.HasFlag(UpdateType.Script) || updateSource.HasFlag(UpdateType.Trigger) || updateSource.HasFlag(UpdateType.Terminal)) {
                if(cmdline.TryParse(argument)) {
                    Dispatch();
                } else {
                    Log.Error("Failed to parse command " + argument);
                }
            }
             

            if((updateSource & UpdateType.Update100) == UpdateType.Update100) {
                if(decom_countdown != -1) {
                    decom_light.BlinkIntervalSeconds = decom_countdown > 1 ? 0.5F : 0.3F;
                    decom_countdown -= 1;
                    if(decom_countdown == -1) {
                        foreach(var sound in sound_blocks) {
                            sound.SelectedSound = sound_b4_alarm;
                            if(playing_b4_alarm) {
                                sound.Play();
                            } else {
                                sound.Stop();
                            }
                        }
                        FinishDecomCountdown();
                    }
                }
            }
        }

        public void Dispatch() {
            string command = cmdline.Argument(0);
            if(command == null) {
                Log.Error("no command");
                return;
            }
            
            Action action;
            if(!commands.TryGetValue(command, out action) ) {
                Log.Error("undefined command " + command);
                return;
            }

            action();
        }

        private void Preflight() {
            if(checklist_sm == null) {
                checklist_sm = checklist.Run();
                Runtime.UpdateFrequency |= UpdateFrequency.Update10;
            } else {
                checklist_sm.Dispose();
                checklist_sm = null;
                checklist.Reset();
            }
        }

        private void CabinDoorToggle() {
            if(decom_countdown != -1) { return; }
            if(cabin_door.Status != DoorStatus.Closed) {
                Log.Put("cabin door seal");
                CabinDoorClose();
                return;
            }

            detected.Clear();
            int num_crew_in_cabin = 0;
            bool enemy = false;
            cabin_sensor.DetectedEntities(detected);
            foreach(var entity in detected) {
                if(entity.Relationship != MyRelationsBetweenPlayerAndBlock.Enemies) {
                    num_crew_in_cabin += 1;
                } else {
                    enemy = true;
                }
            }

            bool pressurized = false;
            foreach(var vent in vents) {
                if(vent.GetOxygenLevel() > 0.001F) { pressurized = true; break; }
            }
            
            if(pressurized && num_crew_in_cabin > 1) {
                if(!enemy) {
                    Log.Put(">1 crew in cabin - await decom. sequence");
                    decom_light.Enabled = true;
                    decom_light.BlinkIntervalSeconds = 0.5F;
                    decom_countdown = 3;
                    foreach(var sound in sound_blocks) {
                        sound_b4_alarm = sound.SelectedSound;
                        playing_b4_alarm = sound.IsSoundSelected;
                        sound.SelectedSound = "Alert 2";
                        sound.Enabled = true;
                        sound.Play();
                    }
                    return;
                } else {
                    Log.Warn("hostiles present in cabin - skip decom. sequence");
                }
            }
            
            FinishDecomCountdown();
        }

        private void FinishDecomCountdown() {
            decom_light.Enabled = false;
            foreach(var vent in vents) {
                vent.Depressurize = true;
            }

            onVentDepressurize = () => {
                Log.Put("green for cabin door egress");
                cabin_door.Enabled = true;
                cabin_door.OpenDoor();
                foreach(var vent in vents) { vent.Depressurize = false; }
                onDoorStateChange = () => {
                    cabin_door.Enabled = false;
                };
            };

        }

        private void CabinDoorClose() {
            cabin_door.Enabled = true;
            cabin_door.CloseDoor();
            onDoorStateChange = () => {
                cabin_door.Enabled = false;
                foreach(var vent in vents) { if(vent.CanPressurize) { vent.Depressurize = false; } }
            };
            return;
        }
        
        class PreFlightChecklist {
            IMyTextSurface screen;
            List<IMyThrust> hydro_thrust = new List<IMyThrust>();
            List<IMyGasTank> reserve_tanks = new List<IMyGasTank>();
            List<IMyGasTank> main_tanks = new List<IMyGasTank>();
            List<IMyGasTank> oxygen_tanks = new List<IMyGasTank>();
            List<IMyLandingGear> gears = new List<IMyLandingGear>();
            IMyShipConnector connector;
            IMyShipController cockpit;
            
            [Flags]
            enum State {
                EmergencyHydroTanksStockpiled = 1,
                MainHydroTanksNonStockpile = 2,
                HydroThrusterOn = 4,
                OxygenTanksNotSetToStockpile = 8,
                Undocked = 16,
            }

            public enum ShouldRender {
                Render,
                Unchanged,
            }

            State state = 0;

            public PreFlightChecklist(IMyShipController cockpit, IMyGridTerminalSystem gridTerminalSystem, IMyTextSurface _screen) {
                screen = _screen;                
                screen.Font = "Monospace";
                screen.FontSize = 0.9F;
                screen.ClearImagesFromSelection();
                screen.Alignment = TextAlignment.CENTER;
                screen.FontColor = Color.Lime;
                screen.BackgroundColor = Color.Black;

                gridTerminalSystem.GetBlockGroupWithName("HYDRO THRUST").GetBlocksOfType(hydro_thrust);
                gridTerminalSystem.GetBlockGroupWithName("RESERVE HYDROGEN").GetBlocksOfType(reserve_tanks);
                gridTerminalSystem.GetBlockGroupWithName("MAIN HYDROGEN").GetBlocksOfType(main_tanks);
                gridTerminalSystem.GetBlockGroupWithName("OXYGEN TANKS").GetBlocksOfType(oxygen_tanks);
                gridTerminalSystem.GetBlockGroupWithName("LANDING GEAR").GetBlocksOfType(gears);

                nnull(hydro_thrust);
                nnull(reserve_tanks);
                nnull(main_tanks);
                nnull(oxygen_tanks);
                nnull(gears);
                connector = nnull(gridTerminalSystem.GetBlockWithName("CONNECTOR") as IMyShipConnector);
                this.cockpit = cockpit;
            }
            
            public void Reset() {
                state = 0;
                green_renders = 0;
                screen.ContentType = ContentType.SCRIPT;
                screen.WriteText("");
            }

            public IEnumerator<ShouldRender> Run() {
                state = 0;
                State ns = 0;
                screen.ContentType = ContentType.TEXT_AND_IMAGE;
                
                start: while(state != (State)0x1F) {
                    if(state != ns) {
                        state = ns;
                        yield return ShouldRender.Render;
                    } else {
                        yield return ShouldRender.Render;
                    }
                    ns = 0;

                    foreach(var tank in reserve_tanks) {
                        if(tank.FilledRatio < 0.95) { goto start; }
                    }
                    ns |= State.EmergencyHydroTanksStockpiled;

                    foreach(var tank in main_tanks) {
                        if(tank.Stockpile) { goto start; }
                    }
                    ns |= State.MainHydroTanksNonStockpile;
                    
                    foreach(var thrust in hydro_thrust) {
                        if(!thrust.Enabled) { goto start; }
                    }
                    ns |= State.HydroThrusterOn;
                    
                    foreach(var tank in oxygen_tanks) {
                        if(tank.Stockpile) { goto start; }
                    }
                    ns |= State.OxygenTanksNotSetToStockpile;
                    
                    bool docked = connector.Status == MyShipConnectorStatus.Connected;
                    bool landed = false;
                    
                    foreach(var gear in gears) {
                        if(gear.IsLocked) { landed = true; break; }
                    }

                    if(docked || landed) { goto start; }

                    ns |= State.Undocked;
                }
                
                while(Vector3.IsZero(cockpit.MoveIndicator)) {
                    yield return ShouldRender.Render;
                }

                yield break;
            }

            private char CheckSymbol(State s) {
                if((state & s) == 0) { return '??'; }
                else { return '??'; }
            }

            public void RenderScreen() {
                screen.WriteText(RenderText(), false);
            }
            
            int green_renders = 30;
            private string RenderText() {
                var sb = new StringBuilder();
                if(state == (State)0x1F) {
                    green_renders = green_renders <= 0 ? 30 : green_renders - 1;
                    for(int i = 0; i < green_renders / 3; ++i) {
                        sb.Append('\n');
                    }
                    sb.Append("GREEN TO LAUNCH");
                    return sb.ToString();
                }

                 
                for(State i = State.EmergencyHydroTanksStockpiled; i < State.Undocked + 1; i = (State)((int)i << 1)) {
                    sb.Append(CheckSymbol(i));
                    sb.AppendLine(StateDescriptionString(i));
                }

                return sb.ToString();
            }

            private string StateDescriptionString(State s) {
                switch(s) {
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
