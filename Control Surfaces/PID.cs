using System;

namespace IngameScript {
    public class PID {
        public float P, I, D;
        float _ts, _tsInv, _errsum, _lasterr;
        bool first = true;

        public PID(float p, float i, float d, float ts = 0.016F) {
            P = p;
            I = i;
            D = d;
            _ts = ts;
            _tsInv = _ts / 1;
            _errsum = 0;
        }

        public float Step(float err) {
            float tD = (err - _lasterr) * _tsInv;
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
