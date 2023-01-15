using System.Collections.Generic;
using System;

namespace IngameScript {
    /// <summary>
    /// Lightweight process interface used to implement multi-tick procedures using <c>IEnumerator</c> and 
    /// built-in language support for state machines using <c>yield</c>
    /// </summary>
    public abstract class IProcess {

        /// <summary>
        /// Empty record type that fills the generic type parameter of <c>IEnumerator</c>
        /// </summary>
        public struct Void {}
        public readonly static Void VOID;
        protected IEnumerator<Void> _prog;
        public IEnumerator<Void> Progress {
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
        
        public IEnumerator<Void> GetEnumerator() => Progress;
        
        /// <summary>
        /// Begin the process, disposing of the state machine if the process was already running
        /// </summary>
        public void Begin() => Progress = Run();

        /// <summary>
        /// Stop the process, disposing of the state machine
        /// </summary>
        public void Stop() => Progress = null;

        protected abstract IEnumerator<Void> Run();
        
        /// <summary>
        /// Poll this process, progressing the state machine and cleaning up the process if
        /// complete
        /// </summary>
        /// <returns>
        /// <c>true</c> if the process has finished executing, and <c>false</c> if the process
        /// still needs further calls to <c>Poll</c> to complete
        /// </returns>
        public virtual bool Poll() {
            if(Progress == null) { return true; }
            bool more = Progress.MoveNext();

            if(more) {
                return false;
            } else {
                Progress = null;
                return true;
            }
        }
    }
    
    /// <summary>
    /// An implementation of <c>IProcess</c> that contains a reference to a method
    /// allowing a process to be easily constructed from a method without boilerplate process API implementation
    /// </summary>
    public sealed class MethodProcess: IProcess {
        Func<IEnumerator<Void>> _func;
        public MethodProcess(Func<IEnumerator<Void>> func) {
            _func = func;
        }

        protected override IEnumerator<Void> Run() => _func();
    }
}
