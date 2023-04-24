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
        /// Starburst streaks per rotation
        public float ScanDensity = 50;

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
        public bool Locked {
            get { return TrackedEntity != -1 && _contacts.ContainsKey(TrackedEntity); }
        }


        /// The entity to lock automatically
        public long TrackedEntity = -1;

        public Contact Tracked {
            get {
                if(Locked) {
                    return _contacts[TrackedEntity];
                }

                return null;
            }
        }

        float _elevationRange;
        float _azimuthRange;

        public IMyCameraBlock Cam;

        /// A map of entity IDs to their corresponding contact
        Dictionary<long, Contact> _contacts = new Dictionary<long, Contact>();
        
        /// A contact accquired by a ranging raycast with contact time and position data
        public class Contact {
            /// Position and velocity data acquired by a ranging scan
            public MyDetectedEntityInfo Body;
            /// Last ping by a raycast
            public double Time;
        }
        
        /// Progress through the current search pattern, 0 to 1
        float _patternProgress;

        public Seeker(IMyCameraBlock cam) {
            Cam = cam;
            Cam.EnableRaycast = true;

            _elevationRange = _azimuthRange = Cam.RaycastConeLimit;

            _patternProgress = 0f;
        }

        private IEnumerator<Nil> SeekProc() {
            for(;;) {
                _patternProgress += (float)Process.Time * ScanSpeedMultiplier;
                _patternProgress = (_patternProgress > 1) ? 0 : _patternProgress;

                if(!(Cam.RaycastDistanceLimit == -1 || Cam.RaycastDistanceLimit >= ScanRange)) {
                    yield return Nil._;
                }


                switch(Mode) {
                    case ScanMode.RangeWhileScanStarburst: {
                        float angle = _patternProgress * 2f * (float)Math.PI;
                        float len = (float)(1f - (float)Math.Abs(2f - ScanDensity * _patternProgress % 4));

                        float pitch = len * ScanElevationRange * (float)Math.Sin(angle) + ScanElevation;
                        float yaw = len * ScanAzimuthRange * (float)Math.Cos(angle) + ScanAzimuth;

                        var info = Cam.Raycast(ScanRange, pitch, yaw);
                        if(!info.IsEmpty() && info.Relationship != MyRelationsBetweenPlayerAndBlock.Neutral) {
                            long id = info.EntityId;
                            _contacts[id].Body = info;
                            _contacts[id].Time = Process.Time;
                            if(info.EntityId == TrackedEntity) {
                                Mode = ScanMode.SingleTargetTrackPredictive;
                                yield return Nil._;
                            }
                        }
                    } break;

                    case ScanMode.SingleTargetTrackPredictive: {
                        var tracked = _contacts[TrackedEntity];
                        float secondsSincePing = (float)(Process.Time - tracked.Time);
                        var expected = (tracked.Body.Position + tracked.Body.Velocity * secondsSincePing);

                        var cast = Cam.Raycast(expected);
                        
                        if(cast.IsEmpty() || cast.EntityId != TrackedEntity) {
                            cast = Cam.Raycast(tracked.Body.Position);
                        }
                        

                        if(cast.IsEmpty() || cast.EntityId != TrackedEntity) {
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
                                yield return Nil._;
                            }

                            cast = Cam.Raycast(ScanRange, rotated);
                        }

                        if(!cast.IsEmpty() && cast.EntityId == TrackedEntity) {
                            tracked.Time = Process.Time;
                            tracked.Body = cast;
                        }

                        yield return Nil._;
                    } break;
                }

                yield return Nil._;
            }
        }
    }

}
