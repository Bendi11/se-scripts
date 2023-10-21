using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;

/// Enumeration that must be returned from all Tasks, signalling different
/// operations to the controlling ProcessManager 
public enum Yield {
    Continue,
    Await,
    Kill,
    /// Poisons any task that was waiting on panicking task
    Exit,
}


/// Internal state for a single task, containing the return value
public class Task {
    /// Used for both the return value of the task and for notification values received by the task
    public object Scratch;
    /// The state machine built by the C# compiler
    public IEnumerator<Yield> Process;
    /// The last returned task status of the process
    public Yield Status;
    /// Handle to the task that is waiting on this task to complete
    public Task Waiter;

    public Task(IEnumerator<Yield> proc) {
        Scratch = null;
        Process = proc;
        Waiter = null;
        Status = Yield.Continue;
    }
}


/// A global process manager that effectively virtualizes the programmable block, allowing multiple
/// tasks to be run concurrently, perform non-blocking operations by sleeping and waking tasks, and run
/// complex operations over multiple game ticks automatically
public static class Tasks {
    /// A global time tracker updated every tick, tracks time since the PB was first run
    public static long Time = 0;
    
    /// Runtime info
    public static IMyGridProgramRuntimeInfo Runtime;
    
    /// Active tasks
    static List<Task> _procs = new List<Task>();
    /// Timers set to wake tasks at a given timestamp 
    static SortedList<double, Task> _timers = new SortedList<double, Task>();

    /// Handle of the currently executing task
    public static Task Current {
        get;
        private set;
    } = null;
    
    /// Excecute the given method asynchronously, call Receive after to receive the result of the method
    public static Yield Async(IEnumerator<Yield> proc) {
        var task = new Task(proc);
        task.Waiter = Current;
        Spawn(task);
        return Yield.Await;
    }
    
    /// Initialize all shared state required to execute tasks
    public static void Init(IMyGridProgramRuntimeInfo runtime) {
        Runtime = runtime;
    }
    
    /// Run the main method with the given time step from a Runtime
    public static void RunMain() {
        Time += (long)Runtime.TimeSinceLastRun.TotalMilliseconds;
        var first = _timers.FirstOrDefault();
        if(first.Value != null) {
            Runtime.UpdateFrequency |= UpdateFrequency.Once;
            if(first.Key <= Time) {
                _timers.Remove(first.Key);
                Wake(first.Value);
            }
        }

        for(int i = _procs.Count - 1; i >= 0; --i) {
            TickManual(_procs[i]); 
        }
    }
    
    /// Execute another tick of the given task
    public static void TickManual(Task task) {
       if(task.Status != Yield.Continue) {
           return;
       }
        
       var oldCurrent = Current;
       Current = task;
       bool more = task.Process.MoveNext();
       Current = oldCurrent;
       task.Status = task.Process.Current;
       if(!more || task.Status == Yield.Kill) {
           Kill(task);
       }
       else Runtime.UpdateFrequency |= UpdateFrequency.Once;

       if(task.Status == Yield.Await) {
           Sleep(task);
       } else if(task.Status == Yield.Exit) {
           Exit(task);
       }
    }

    /// Put the given task to sleep
    public static void Sleep(Task task) {
        task.Status = Yield.Await;
        _procs.Remove(task);
    }
    
    /// Kill the given task
    public static void Kill(Task task) {
        task.Status = Yield.Kill;
        task.Process.Dispose();
        task.Process = null;
        _procs.Remove(task);
        if(task.Waiter != null) {
            if(task.Scratch != null) {
                Notify(task.Waiter, task.Scratch);
            } else {
                Kill(task.Waiter);
            }
            task.Waiter = null;
        }
    }
    
    /// Force the given task to stop, also killing any tasks that were waiting on the task to finish
    public static void Exit(Task task) {
        task.Status = Yield.Exit;
        if(task.Process != null) {
            task.Process.Dispose();
            task.Process = null;
        }
        _procs.Remove(task);
        if(task.Waiter != null) {
            Exit(task.Waiter);
            task.Waiter = null;
        }
    }
    
    /// Sleep the current task until the given timespan has passed
    public static Yield WaitMs(long ms) {
        long time = Time + ms;
        for(;;) {
            if(_timers.ContainsKey(time)) {
                time += 1;
            } else {
                _timers.Add(time, Current);
                break;
            }
        }
        return Yield.Await;
    }
    
    /// Return a value from this task, notifying any tasks that are awaiting a value from it
    public static Yield Return<T>(T value) {
        Current.Scratch = value;
        return Yield.Kill;
    }
    
    /// Wake the given process by reference
    public static void Wake(Task task) {
        task.Status = Yield.Continue;
        _procs.Add(task);
        Runtime.UpdateFrequency |= UpdateFrequency.Once;
    }
    
    /// Notify a task of a new value, waking it from an await
    public static void Notify(Task task, object val) {
        if(task.Status != Yield.Kill) {
            task.Scratch = val;
            Wake(task);
        }
    }
    
    /// Get a received notification from another task
    public static T Receive<T>() => (T)Current.Scratch;

    /// Spawn the given task on this runtime, allowing it to run until completion
    public static Task Spawn(IEnumerable<Yield> process) => Spawn(process.GetEnumerator());
    public static Task Spawn(IEnumerator<Yield> p) => Spawn(new Task(p));
    static Task Spawn(Task process) {
        _procs.Add(process);
        return process;
    }
}
