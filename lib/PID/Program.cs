
public sealed class PID {
    float P { get; set; }
    float I { get; set; }
    float D { get; set; }

    float errsum = 0;
    float ts;
    float tsInv;
    float last_err = float.NaN;
    float i_decay = 0F;

    public PID(float kp, float ki, float kd, float idec = 0F, float time = 0.016F) {
        P = kp;
        I = ki;
        D = kd;
        ts = time;
        tsInv = 1f / ts;
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
        float i = errsum * (1F - i_decay) + error * ts;
        
        float d = (error - last_err) * tsInv;
        if(float.IsNaN(last_err)) {
            d = 0;
        }

        float output = (P * error) + (I * i) + (D * d);
        last_err = error;
        return output;
    }
}
