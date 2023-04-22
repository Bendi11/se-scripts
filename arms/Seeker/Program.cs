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
        GyroController gyro;
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
                gyro.Rate = 0.3f;
                
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
                thrust.Rate = 2f;
                
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

                    if(seeker.Locked) { 
                        gyro.Enable();
                        thrust.Enabled = true;


                        var dist = Vector3.Distance(seeker.Tracked.body.HitPosition.Value, seeker.Cam.GetPosition());
                        float secondsSincePing = (float)(seeker.Ticks - seeker.Tracked.tick) * 0.016f;

                        if(dist <= 1 || (dist <= 5 && secondsSincePing > 1)) {
                            foreach(var bomb in warheads) {
                                bomb.Detonate();
                            }
                        }
                        Vector3D desiredVel = Vector3D.TransformNormal(
                            seeker.Tracked.body.HitPosition.Value - seeker.Cam.GetPosition(),
                                MatrixD.Transpose(seeker.Cam.WorldMatrix)
                        ).Normalized();

                        thrust.VelLocal = desiredVel * 35;
                        
                        var currentVel = Vector3D.TransformNormal(seeker.Cam.CubeGrid.LinearVelocity, MatrixD.Transpose(seeker.Cam.WorldMatrix));
                        gyro.OrientLocal = desiredVel - currentVel / 5;
                    } else {
                        thrust.Enabled = false;
                        gyro.Disable();
                    }

                    Runtime.UpdateFrequency |= UpdateFrequency.Once;
                }
            } catch(Exception e) {
                Log.Panic(e.Message);
                thrust.Enabled = false;
                gyro.Disable();
            }
        }
    }
}
