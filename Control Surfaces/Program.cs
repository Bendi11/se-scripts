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
          
        public Program() {
             
        }

        public void Save() {
            
        }

        public void Main(string argument, UpdateType updateSource) {
             
        }
    }

    class PID {
        public double P, I, D;
        double _ts, _tsInv, _errsum, _lasterr;
        bool first = true;

        public PID(double p, double i, double d, double ts = 0.16) {
            P = p;
            I = i;
            D = d;
            _ts = ts;
            _tsInv = _ts / 1;
            _errsum = 0;
        }

        public double Step(double err) {
            double tD = (err - _lasterr) * _tsInv;
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
