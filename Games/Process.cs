using System.Collections.Generic;

/// Enumeration that must be returned from all Tasks, signalling different
/// operations to the controlling ProcessManager 
public enum Yield {
    Continue,
    Await,
    Kill,
}


/// Internal state for a single task plate, containing the return value
public class Task {
    public object Scratch;
    public IEnumerator<Yield> Process;
    public Yield Status;
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
public static class ProcessManager {
    /// A global time tracker updated every tick, tracks time since the PB was first run
    public static double Time = 0;
    
    /// Active tasks
    static HashSet<Task> _procs = new HashSet<Task>();
    /// Process ID generator
    static int _pid = 1;
    
    /// A process ID signifying that the given ID is invalid
    public const int INVALID_PID = 0;
    
    /// ID of the currently executing task
    static private Task _currentTask = null;

    /// Get the currently executing task
    public static Task Current {
        get { return _currentTask; }
    }
    
    /// Excecute the given method asynchronously, call Receive after to receive the result of the method
    public static Yield Async<T>(IEnumerator<Yield> proc) {
        var task = new Task(proc);
        task.Waiter = Current;
        Spawn(task);
        return Yield.Await;
    }
    
    /// Run the main method
    public static void RunMain(double timeStep) {
        Time += timeStep;
        _procs.RemoveWhere(t => t.Status != Yield.Continue);
        foreach(var task in _procs) {
            _currentTask = task;
            bool more = task.Process.MoveNext();
            _currentTask = null;
            if(!more) Kill(task);
            task.Status = task.Process.Current;
        }
    }
    
    /// Put the given task to sleep
    public static void Sleep(Task task) {
        task.Status = Yield.Await;
        _procs.Remove(task);
    }
    
    /// Kill the given task
    public static void Kill(Task task) {
        task.Process.Dispose();
        task.Process = null;
        if(task.Waiter != null && task.Scratch != null) {
            Notify(task.Waiter, task.Scratch);
        }
    }

    /// Exit the process without a return value
    public static void Die() => Kill(Current);
    
    /// Return a value from this task, notifying any tasks that are awaiting a value from it
    public static void Return<T>(T value) {
        Current.Scratch = value;
        Kill(Current);
    }
    
    /// Wake the given process by reference
    public static void Wake(Task task) {
        task.Status = Yield.Continue;
        _procs.Add(task);
    }
    
    /// Notify a task of a new value, waking it from an await
    public static void Notify(Task task, object val) {
        if(task.Status != Yield.Kill && task.Scratch == null) {
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
