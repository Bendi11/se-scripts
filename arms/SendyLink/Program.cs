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

        [Flags]
        public enum Capabilities {
            None = 0,
            /// Target acquisition
            Acquisition = 1,
            /// Torpedo behavior (target intercept)
            Torpedo = 2,
        }

        [Flags]
        public enum AcquisitionCapabilities {
            None = 0,
            /// Multi-target area search
            Search = 1,
            /// Single-target tracking providing more frequent updates on a single target
            STT = 2,
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
            /// Same as `Target`, but vector is acquired using onboard STT lock
            TargetLocal,
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

        public enum Request {
            /// resp: string
            Name,
            /// resp: string
            Description,
            /// req: null, resp: Capabilities
            Capabilities,
            /// req: SPIMode
            SPIModeSet,
            /// Set the device's sensor-point-of-interest
            /// req: TargetData
            SPISet,

            /// null
            Enable,
            /// null
            Disable,

            /// Capabilities.Acquisition
            /// resp: AcquisitionCapabilities
            AcquisitionCapabilities,
            /// AcquisitionCapabilities.Search
            /// req: null, resp: ImmutableList<TargetData>
            ListTargets,

            /// AcquisitionCapabilities.STT
            /// Sets SPI mode to SPIMode.TargetLocal
            /// resp: TargetData (periodic)
            LockTarget,
            /// AcquisitionCapabilities.STT
            /// null
            DropLock,

            /// Capabilities.Torpedo
            /// Arm payload of a torpedo
            Arm,
            
            /// Capabilities.Torpedo
            /// Fire torpedo at SPI
            Fire,
        }
    }
}
