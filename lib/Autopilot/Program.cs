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
    /// Autopilot using PIDs to control each axis of the velocity vector
    /// </summary>
    public class Autopilot {
        public Vector3D PositionWorld {
            set { PositionLocal = Vector3D.TransformNormal(value - Ref.GetPosition(), MatrixD.Transpose(Ref.WorldMatrix)); }
            get { return Vector3D.Transform(PositionLocal, Ref.WorldMatrix); }
        }
        public Vector3D PositionLocal;

        public IMyCubeBlock Ref;
        PID _x, _y, _z;

        Thrust _thrust;

        public bool Enabled {
            get { return _thrust.Enabled; }
            set { _thrust.Enabled = value; }
        }

        public Autopilot(IMyGridTerminalSystem GTS, IMyShipController _ref) {
            _thrust = new Thrust(GTS, _ref);
            Ref = _ref;
            _x = new PID(1, 0, 0);
            _y = new PID(1, 0, 0);
            _z = new PID(1, 0, 0);
        }

        public void UpdateMass() => _thrust.UpdateMass();

        public void Step() {
            Vector3D error = PositionLocal - Ref.GetPosition();
            Vector3D control = new Vector3D(
                _x.Step(error.X),
                _y.Step(error.Y),
                _z.Step(error.Z)
            );
        }
    }

    class PID {
        public double P, I, D;
        double _ts, _ts_inv, _errsum, _lasterr;
        bool first = true;

        public PID(double p, double i, double d, double ts = 0.16) {
            P = p;
            I = i;
            D = d;
            _ts = ts;
            _ts_inv = _ts / 1;
            _errsum = 0;
        }

        public double Step(double err) {
            double tD = (err - _lasterr) * _ts_inv;
            if(first) {
                tD = 0;
                first = false;
            }

            _errsum = _errsum + err * _ts;
            _lasterr = err;
            return P * err + I * _errsum + D * tD;
        }
    }
}
