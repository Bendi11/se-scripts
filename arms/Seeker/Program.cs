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
    /// A seeker head utilizing a camera to raycast to accquire and track other ships
    class Seeker {
        /// Different types of scan patterns that can be utilized to provide target accquisition and tracking data
        public enum ScanMode {
            /// Provides only target accquisition and ranging data while also scanning for new contacts
            ///
            /// Searches in a starburst pattern across the camera's FOV
            RangeWhileScanStarburst,
            
            /// Provides STT-lock functionality, foregoing scan for new contacts in order to accurately range a selected target
            SingleTargetTrackPredictive,
        }
        
        public ScanMode Mode = ScanMode.RangeWhileScanStarburst;
        /// Scan steps per second
        public float ScanSpeedMultiplier = 1f;
        public double ScanRange = 100;
        public float ScanElevationRange {
            get { return _elevationRange; }
            set {
                var remainRange = Cam.RaycastConeLimit - Math.Abs(ScanElevation);
                if(remainRange < value) {
                    value = remainRange;
                }
                _elevationRange = value;
            }
        }
        public float ScanElevation = 0;
        public float ScanAzimuthRange {
            get { return _azimuthRange; }
            set {
                var remainRange = Cam.RaycastConeLimit - Math.Abs(ScanAzimuth);
                if(remainRange < value) {
                    value = remainRange;
                }
                _azimuthRange = value;
            }
        }
        public float ScanAzimuth = 0;
        public readonly Process<Nil> Seek;
        public bool Locked {
            get { return _trackedEntity != -1; }
        }

        public Contact Tracked {
            get {
                if(Locked) {
                    return _contacts[_trackedEntity];
                }

                return null;
            }
        }

        float _elevationRange;
        float _azimuthRange;
        public long Ticks = 0;

        IMyGridProgramRuntimeInfo _rt;
        public IMyCameraBlock Cam;
        /// A map of entity IDs to their corresponding contact
        Dictionary<long, Contact> _contacts = new Dictionary<long, Contact>();
        
        /// A contact accquired by a ranging raycast with contact time and position data
        public class Contact {
            /// Position and velocity data acquired by a ranging scan
            public MyDetectedEntityInfo body;
            /// Last ping by a raycast
            public long tick;
        }
        
        /// The entity to track with a STT search pattern
        long _trackedEntity = -1;
        /// Progress through the current search pattern, 0 to 1
        float _patternProgress;

        public Seeker(IMyGridTerminalSystem gts, IMyProgrammableBlock me, IMyGridProgramRuntimeInfo rt) {
            List<IMyCameraBlock> cams = new List<IMyCameraBlock>();
            gts.GetBlocksOfType(
                cams,
                (cam) => MyIni.HasSection(cam.CustomData, "seeker") && cam.IsSameConstructAs(me)
            );

            if(cams.Count != 1) {
                Log.Panic("Must have exactly one block with a [seeker] attribute in custom data");
            }

            Cam = cams.First();
            Cam.EnableRaycast = true;

            _elevationRange = _azimuthRange = Cam.RaycastConeLimit;

            _patternProgress = 0f;
            _rt = rt;

            Seek = new MethodProcess(SeekProc);
        }

        public void Tick() {
            Ticks += 1;
        }

        private IEnumerator<Nil> SeekProc() {
            for(;;) {
                float time = (float)_rt.TimeSinceLastRun.TotalSeconds;
                _patternProgress += time * ScanSpeedMultiplier;
                _patternProgress = (_patternProgress > 1) ? 0 : _patternProgress;

                if(!(Cam.RaycastDistanceLimit == -1 || Cam.RaycastDistanceLimit >= ScanRange)) {
                    yield return Nil._;
                }


                switch(Mode) {
                    case ScanMode.RangeWhileScanStarburst: {
                        float angle = _patternProgress * 2f * (float)Math.PI;
                        float len = (float)(1f - (float)Math.Abs(2f - 50 * _patternProgress % 4));

                        float pitch = len * ScanElevationRange * (float)Math.Sin(angle) + ScanElevation;
                        float yaw = len * ScanAzimuthRange * (float)Math.Cos(angle) + ScanAzimuth;

                        var info = Cam.Raycast(ScanRange, pitch, yaw);
                        if(!info.IsEmpty() && info.Relationship != MyRelationsBetweenPlayerAndBlock.Neutral) {
                            Log.Put($"{info.Name} @ {info.Position} - {info.Relationship.ToString()}");
                            long id = info.EntityId;
                            Contact ping = new Contact();
                            
                            ping.body = info;
                            ping.tick = Ticks;
                            _contacts[id] = ping;
                            _trackedEntity = id;
                            Mode = ScanMode.SingleTargetTrackPredictive;
                        }
                    } break;

                    case ScanMode.SingleTargetTrackPredictive: {
                        MyDetectedEntityInfo cast = new MyDetectedEntityInfo();

                        var tracked = _contacts[_trackedEntity];
                        float secondsSincePing = (float)(Ticks - tracked.tick) * 0.016f;
                        var expected = (tracked.body.Position + tracked.body.Velocity * secondsSincePing);

                        cast = Cam.Raycast(expected);
                        
                        if(cast.IsEmpty() || cast.EntityId != _trackedEntity) {
                            cast = Cam.Raycast(tracked.body.Position);
                        }
                        

                        if(cast.IsEmpty() || cast.EntityId != _trackedEntity) {
                            Vector3D dir = Vector3D.TransformNormal(
                                expected - Cam.GetPosition(),
                                    MatrixD.Transpose(Cam.WorldMatrix)
                            ).Normalized();

                            float angle = _patternProgress * 2f * (float)Math.PI;
                            float len = (float)(1f - (float)Math.Abs(2f - 50 * _patternProgress % 4));

                            float pitch = len * (float)Math.PI / 50f * (float)Math.Sin(angle);
                            float yaw = len * (float)Math.PI / 50f * (float)Math.Cos(angle);
                            
                            var rot = MatrixD.CreateRotationX(pitch) * MatrixD.CreateRotationY(yaw);
                            Vector3D rotated;
                            Vector3D.Rotate(ref dir, ref rot, out rotated);

                            if(secondsSincePing > 5) {
                                Mode = ScanMode.RangeWhileScanStarburst;
                                _trackedEntity = -1;
                                yield return Nil._;
                            }

                            cast = Cam.Raycast(ScanRange, rotated);
                        }

                        if(!cast.IsEmpty() && cast.EntityId == _trackedEntity) {
                            tracked.tick = Ticks;
                            tracked.body = cast;
                        }
 
                        yield return Nil._;
                    } break;
                }

                yield return Nil._;
            }
        }
    }

}
