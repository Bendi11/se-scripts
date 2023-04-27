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
            /// Unicast from rt to bc
            /// bc: null, rt: RemoteTerminalData
            Connect,
            /// bc: bool, rt: null
            SetEnabled,
            
            LEN,
        }

        public struct RemoteTerminalData {
            public string Name, Description;
            public Device Kind;

            public object Encode() { return MyTuple.Create(Name, Description, (int)Kind); }
            public static Nullable<RemoteTerminalData> Decode(object o) {
                if(o is MyTuple<string, string, int>) {
                    var tuple = (MyTuple<string, string, int>)o;
                    return new RemoteTerminalData() {
                        Name = tuple.Item1,
                        Description = tuple.Item2,
                        Kind = (Device)(int)tuple.Item3
                    };
                }

                return null;
            }
        }

        public enum Device {
            /// A torpedo with independent target tracking
            ActiveTrackingTorpedo,
            /// A torpedo that must be guided via antenna
            RemoteGuidedTorpedo,
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
