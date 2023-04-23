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
        [Flags]
        public enum Capabilities {
            /// Multi-target acquisition
            ACQ = 1,
            /// Point tracking
            TCK = 2,
        }

        public struct TargetData {
            public Vector3D WorldPos;
            public Nullable<Vector3D> WorldVelocity;

            public object Encode() {
                if(WorldVelocity.HasValue) {
                    return WorldPos;
                } else {
                    return MyTuple.Create(WorldPos, WorldVelocity.Value);
                }
            }

            public static Nullable<TargetData> Decode(object o) {
                var self = new TargetData();
                if(o is Vector3D) { self.WorldPos = (Vector3D)o; }
                else if(o is MyTuple<Vector3D, Vector3D>) {
                    var tuple = (MyTuple<Vector3D, Vector3D>)o;
                    self.WorldPos = tuple.Item1;
                    self.WorldVelocity = tuple.Item2;
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
            
            /// null
            Enable,
            /// null
            Disable,
            
            /// req: null, resp: ImmutableList<TargetData>
            Targets,
        }
    }
}
