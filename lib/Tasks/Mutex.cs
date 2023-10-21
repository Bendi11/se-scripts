using System.Collections.Generic;

/// A mutex allowing only a single task access to a resource at once
public class Mutex {
    bool _lock;
    List<Task> _waiting;

    public Mutex() {
        _lock = false;
        _waiting = new List<Task>();
    }
    
    /// Check if the mutex has been locked by another process
    public bool Locked {
        get { return _lock; }
    }
    
    /// Attempt to lock this mutex, waiting for all queued accessors to release their lock
    public Yield Lock() {
        if(Locked) {
            _waiting.Add(Tasks.Current);
            return Yield.Await;
        } else {
            _lock = true;
            return Yield.Continue;
        }
    }
    
    /// Release a held mutex lock, waking the next task that has requested access to this mutex
    public void Unlock() {
        if(_waiting.Count != 0) {
            var task = _waiting[0];
            _waiting.RemoveAt(0);
            Tasks.Wake(task);
        } else {
            _lock = false;
        }
    }
}
