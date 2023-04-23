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
    public sealed class PID {
        float P { get; set; }
        float I { get; set; }
        float D { get; set; }

        float errsum = 0;
        float ts;
        float last_err = float.NaN;
        float i_decay = 0F;

        public PID(float kp, float ki, float kd, float idec = 0F, float time = 0.16F) {
            P = kp;
            I = ki;
            D = kd;
            ts = time;
            i_decay = idec;
        }

        public PID(PID other) {
            P = other.P;
            I = other.I;
            D = other.D;
            errsum = other.errsum;
            ts = other.ts;
            last_err = other.last_err;
            i_decay = other.i_decay;
        }

        public float Run(float error) {
            errsum += error;
            errsum *= (1F - i_decay);
            float i = errsum * (1F - i_decay) * ts;
            
            float d = (error - last_err) / ts;
            if(float.IsNaN(last_err)) {
                d = 0;
            }

            float output = (P * error) + (I * i) + (D * d);
            last_err = error;
            return output;
        }
    }
}
