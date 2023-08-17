using System.Collections.Generic;

partial class ShipCore {
    List<IEnumerator<PS>> _ps = new List<IEnumerator<PS>>();
    int _progress;
    /// The current time in seconds, set by the runtime each tick
    public double Time;
    //Maximum instruction count per tick
    int _insLimit;
    
    /// <summary>
    /// Make progress on all processes, staging operations to use the maximum
    /// instruction count available per tick
    /// </summary>
    public void RunMain() {
        int start = _progress;
        while(Runtime.CurrentInstructionCount < _insLimit) {
            var proc = _ps[_progress];
            if(!proc.MoveNext()) {
                proc.Dispose();
                _ps.RemoveAt(_progress);
            } else {
                _progress += 1;
            }

            if(_progress >= _ps.Count) {
                _progress = 0;
            }
            if(_progress == start) {
                break;
            }
        }
    }

    public void Spawn(IEnumerator<PS> proc) => _ps.Add(proc);
}

public enum PS {
    /// The process can continue executing on the next tick
    Execute,
    /// The process is awaiting an event from the scheduler to wake up
    Suspend,
}
