using System.Collections.Generic;
using System;

namespace IngameScript {
    /// <summary>
    /// Empty record type that fills the generic type parameter of <c>IEnumerator</c>
    /// </summary>
    public struct Nil { public static readonly Nil _; }

    /// <summary>
    /// Lightweight process interface used to implement multi-tick procedures using <c>IEnumerator</c> and 
    /// built-in language support for state machines using <c>yield</c>
    /// </summary>
    public abstract class Process<T> {
        protected IEnumerator<T> _prog;
        protected T _val;
        protected double _time;

        public T Value {
            get { if(InProgress) throw new Exception("process not complete"); return _val; }
        }
        public IEnumerator<T> Progress {
            get { return _prog; }
            set {
                if(_prog != null) {
                    _prog.Dispose();
                }
                _prog = value;
            }
        }
        public bool InProgress {
            get {
                return Progress != null;
            }
        }
        
        /// <summary>
        /// Begin the process, disposing of the state machine if the process was already running
        /// </summary>
        public virtual void Begin() { Progress = Run(); }

        /// <summary>
        /// Stop the process, disposing of the state machine
        /// </summary>
        public virtual void Stop() { Progress = null; }

        protected abstract IEnumerator<T> Run();
        
        /// <summary>
        /// Poll this process, progressing the state machine and cleaning up the process if
        /// complete
        /// </summary>
        /// <returns>
        /// <c>true</c> if the process has finished executing, and <c>false</c> if the process
        /// still needs further calls to <c>Poll</c> to complete
        /// </returns>
        public virtual bool Poll(double time) {
            _time = time;
            if(Progress == null) { return true; }
            bool more = Progress.MoveNext();

            if(more) {
                return false;
            } else {
                _val = Progress.Current;
                Progress = null;
                return true;
            }
        }
    }

    public abstract class Process: Process<Nil> {}
    
    /// <summary>
    /// An implementation of <c>IProcess</c> that contains a reference to a method
    /// allowing a process to be easily constructed from a method without boilerplate process API implementation
    /// </summary>
    public class MethodProcess<T>: Process<T> {
        Func<IEnumerator<T>> _func;
        Action _start;
        Action _end;
        
        /// <summary>
        /// Create a new <c>MethodProcess</c> from the given process method, plus optional setup and takedown functions
        /// </summary>
        public MethodProcess(Func<IEnumerator<T>> func, Action start = null, Action end = null) {
            _func = func;
            _start = start;
            _end = end;
        }

        public override void Begin() {
            if(_start != null) _start();
            base.Begin();
        }

        public override void Stop() {
            if(_end != null) _end();
            base.Stop();
        }
        protected override IEnumerator<T> Run() => _func();
    }

    public class MethodProcess: MethodProcess<Nil> { public MethodProcess(Func<IEnumerator<Nil>> f, Action s=null, Action e=null) : base(f,s,e) {} }
}
