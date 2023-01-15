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
    /// <summary>
    /// A single connection with another programmable block, keeping track of 
    /// connection status with pings and maintining a dictionary of tags to the actions
    /// that should be dispatched
    /// </summary>
    class Connection {
        public readonly long
            Node,
            TicksWithoutPing;

        public readonly ImmutableDictionary<string, IDispatch> Actions;

        public Connection(long addr) {
            Node = addr;
            TicksWithoutPing = 0;
        } 
    }

    interface IDispatch {
        bool Validate(object data);
        void ExecuteRaw(Connection c, object data);
    }

    class Dispatch<T>: IDispatch where T: class {
        Action<Connection, T> _f;
        public Dispatch(Action<Connection, T> f) {
            _f = f;
        }
        public bool Validate(object d) => d as T != null; 
        public void ExecuteRaw(Connection c, object d) => _f(c, (T)d);
    } 
}
