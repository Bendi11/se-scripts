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
    public static class SendyLink {
        public const string DOMAIN = "sl";

        public enum Command {
            /// bc: null, rt: string
            Name,
            /// bc: null, rt: string
            Description,
            /// bc: null, rt: Device
            Kind,
            /// bc: bool, rt: null
            SetEnabled,
            
            LEN,
        }

        public enum Device {
            /// Targeting pod allowing wide field-of-view acquisition with a camera
            TGP,
            /// A fixed radar with adjustable elevation and azimuth that can search and track targets
            SearchTrackRadar,
            /// A torpedo with independent target tracking
            ActiveTrackingTorpedo,
            /// A torpedo that must be guided via antenna
            RemoteGuidedTorpedo,
        }

        public static class TGP {
            public enum Command {
                FIRST = SendyLink.Command.LEN + 1,
                /// Cast a ray from the tgp camera and point lock a target if detected
                /// bc: double (distance), rt: TargetData?
                PointLock,
                /// Get the current targeting mode
                /// bc: null, rt: SPIMode
                GetSPIMode,
                /// Set the current targeting mode
                /// bc: SPIMode, rt: null
                SetSPIMode,
                /// Set a vector representing the SPI, can be a local or world direction or a 
                /// world position
                /// bc: TargetData, rt: null
                SetSPI,
                /// Get the current SPI location / direction
                /// bc: null, rt: TargetData
                GetSPI,
            }
        }

        public static class SearchTrackRadar {
            public enum Command {
                FIRST = SendyLink.Command.LEN,
                /// Set the current tracking mode
                /// bc: Mode, rt: null
                SetMode,
                
                /// Get a list of the currently detected entities
                /// bc: null, rt: ImmutableList<TargetData>
                SearchList,
                /// Get the currently-tracked target
                /// bc: null, rt: TargetData?
                GetTracked,

                /// Get the direction that the radar points in world coordinates
                /// bc: null, rt: Vector3D
                GetFacing,

                /// bc: null, rt: double
                GetEl,
                /// bc: double, rt: null
                SetEl,
                /// bc: null, rt: double
                GetAz,
                /// bc: double, rt: null
                SetAz,
                
                /// bc: double, rt: null
                SetElLim,
                /// bc: null, rt: double
                GetElLim,
                /// bc: double, rt: null
                SetAzLim,
                /// bc: null, rt: double
                GetAzLim,
            }
            
            /// Targeting modes for radar
            public enum Mode {
                /// Range-while-scan: search for targets in the FOV
                RWS,
                /// Single-target-track: track a single target
                STT,
            }
        }

        public enum SPIMode {
            /// Vector3D represents a local direction
            LocalDir,
            /// Vector representing a world direction
            WorldDir,
            /// Vector representing a world position
            WorldPos,
            /// Vector representing a (potentially) moving target with entity id and velocity to track
            Target,
        }

        public struct TargetData {
            /// -1 if no entity
            public long EntityId;
            /// Vector representing direction or position in world or local coordinates
            public Vector3D Vec;
            public Nullable<Vector3D> WorldVelocity;

            public object Encode() {
                if(WorldVelocity.HasValue) {
                    return MyTuple.Create(EntityId, Vec);
                } else {
                    return MyTuple.Create(EntityId, Vec, WorldVelocity.Value);
                }
            }

            public static Nullable<TargetData> Decode(object o) {
                var self = new TargetData();
                if(o is MyTuple<long, Vector3D>) {
                    var tuple = (MyTuple<long, Vector3D>)o;
                    self.EntityId = tuple.Item1;
                    self.Vec = tuple.Item2;
                }
                else if(o is MyTuple<long, Vector3D, Vector3D>) {
                    var tuple = (MyTuple<long, Vector3D, Vector3D>)o;
                    self.EntityId = tuple.Item1;
                    self.Vec = tuple.Item2;
                    self.WorldVelocity = tuple.Item3;
                } else {
                    return null;
                }

                return self;
            }
        }
    }
}
