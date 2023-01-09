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
        sealed class PID {
            float P { get; set; }
            float I { get; set; }
            float D { get; set; }

            float last_integral = 0;
            float ts;
            float last_err = 0;
            float i_decay = 0F;

            public PID(float kp, float ki, float kd, float idec = 0F, float time = 0.16F) {
                P = kp;
                I = ki;
                D = kd;
                ts = time;
                i_decay = idec;
            }

            public float Run(float error) {
                float i = last_integral * (1F - i_decay) + error * ts;
                last_integral = i;
                float output = (P * error) + (I * i) + (D * ((error / last_err) / ts));
                last_err = error;
                return output;
            }
        }
    }
}
