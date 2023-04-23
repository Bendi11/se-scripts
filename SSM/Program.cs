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
        Seeker seeker;
        IMySensorBlock sensor;
        GyroController gyro;
        IMyShipConnector connector;
        Thrust thrust;
        List<IMyWarhead> warheads;
        uint scansPerTick = 1;
        float lastDist = 0;
         
        public Program() {
            Log.Init(Me.GetSurface(0));

            try {
                seeker = new Seeker(GridTerminalSystem, Me, Runtime);
                List<IMyGyro> gyros = new List<IMyGyro>();
                GridTerminalSystem.GetBlocksOfType(gyros, (gyro) => gyro.IsSameConstructAs(Me));
                gyro = new GyroController(gyros, seeker.Cam);
                gyro.Pid = new PID(0.5f, 0f, 0f);
                
                MyIni ini = new MyIni();
                if(ini.TryParse(Me.CustomData)) {
                    float mul = (float)ini.Get("seeker", "mul").ToDouble();
                    scansPerTick = ini.Get("seeker", "scans").ToUInt32();
                    seeker.ScanAzimuth = (float)ini.Get("seeker", "az").ToDouble(seeker.ScanAzimuth);
                    seeker.ScanElevation = (float)ini.Get("seeker", "el").ToDouble(seeker.ScanElevation);

                    seeker.ScanAzimuthRange = (float)ini.Get("seeker", "azr").ToDouble(seeker.ScanAzimuthRange);
                    seeker.ScanElevationRange = (float)ini.Get("seeker", "elr").ToDouble(seeker.ScanElevationRange);
                    seeker.ScanSpeedMultiplier = mul;
                }
                
                warheads = new List<IMyWarhead>();
                GridTerminalSystem.GetBlocksOfType(warheads, (block) => block.IsSameConstructAs(Me));
                foreach(var bomb in warheads) {
                    bomb.IsArmed = true;
                }

                thrust = new Thrust(GridTerminalSystem, seeker.Cam, 1506); 
                thrust.Rate = 10f;
                
                List<IMySensorBlock> sensors = new List<IMySensorBlock>();
                GridTerminalSystem.GetBlocksOfType(sensors, (block) => block.IsSameConstructAs(Me) && MyIni.HasSection(block.CustomData, "prox"));
                if(sensors.Count != 1) { Log.Panic($"Expecting 1 sensor with a [prox] tag, found {sensors.Count}"); }
                sensor = sensors.First();

                List<IMyShipConnector> connectors = new List<IMyShipConnector>();
                GridTerminalSystem.GetBlocksOfType(connectors, (block) => block.IsSameConstructAs(Me) && MyIni.HasSection(block.CustomData, "hardpoint"));
                if(connectors.Count != 1) { Log.Panic($"Expecting 1 connector with a [hardpoint] tag, found {connectors.Count}"); }
                connector = connectors.First();

                sensor.DetectEnemy =
                    sensor.DetectAsteroids =
                    sensor.DetectNeutral =
                    sensor.DetectPlayers =
                    sensor.DetectLargeShips =
                    sensor.DetectSmallShips =
                    sensor.DetectStations =
                    sensor.DetectFloatingObjects =
                    true;

                sensor.BackExtend = 
                    sensor.RightExtend =
                    sensor.LeftExtend =
                    sensor.BottomExtend =
                    sensor.TopExtend =
                    sensor.BottomExtend =
                    2;
                
                seeker.Seek.Begin();
                Runtime.UpdateFrequency |= UpdateFrequency.Once;
            } catch(Exception e) {
                Log.Panic(e.Message);
            }
        }

        public void Save() {
            
        }

        public void Main(string argument, UpdateType updateSource) {
            try {
                if(updateSource.HasFlag(UpdateType.Once)) {
                    seeker.Tick();
                    gyro.Step();
                    thrust.Step();
                    for(uint i = 0; i < scansPerTick; ++i) {
                        try {
                            seeker.Seek.Poll();
                        } catch(Exception e) { Log.Panic(e.Message); }
                    }

                    if(seeker.Locked && !connector.IsConnected) { 
                        gyro.Enable();
                        if(!thrust.Enabled) {
                            thrust.VelLocal = Vector3D.One * 500;
                            Runtime.UpdateFrequency |= UpdateFrequency.Once;
                            return;
                        }
                        thrust.Enabled = true;
                        
                        var pos = seeker.Tracked.body.Position;
                        var dist = Vector3.Distance(pos, seeker.Cam.GetPosition());
                        float secondsSincePing = (float)(seeker.Ticks - seeker.Tracked.tick) * 0.016f;
                        if(
                                sensor.IsActive && sensor.LastDetectedEntity.EntityId == seeker.Tracked.body.EntityId
                                || (
                                    !sensor.IsWorking &&
                                    dist < 5
                                )
                        ) {
                            foreach(var bomb in warheads) {
                                bomb.Detonate();
                            }
                        }
                        
                        var vR = seeker.Tracked.body.Velocity - seeker.Cam.CubeGrid.LinearVelocity;
                        var r = seeker.Tracked.body.Position - seeker.Cam.GetPosition();

                        var omega = (r.Cross(vR)) / (r.Dot(r));
                        
                        var n = 5;
                        var accel = (n * vR).Cross(omega); 

                        thrust.VelWorld = accel / 0.016f;
                        gyro.OrientWorld = Vector3D.Normalize(accel);
                    } else {
                        thrust.Enabled = true;
                        thrust.VelLocal = Vector3D.Zero;
                        gyro.OrientWorld = -seeker.Cam.CubeGrid.LinearVelocity;
                    }

                    Runtime.UpdateFrequency |= UpdateFrequency.Once;
                }
            } catch(Exception e) {
                Log.Panic(e.ToString());
                thrust.Enabled = false;
                gyro.Disable();
            }
        }
    }
}
