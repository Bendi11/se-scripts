using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System;
using System.Collections.Immutable;
using VRage;

namespace IngameScript {
    /// <summary>
    /// General purpose unicast communications protocol with boilerplate
    /// reducing methods to register actions for specific requests
    /// </summary>
    public abstract class Sendy<Method> {
        public delegate void ReceiveFunc(long addr, object data);

        public readonly IMyIntergridCommunicationSystem IGC;
        public IMyBroadcastListener Broadcast = null;

        long _msgNo = 1;
        Dictionary<long, AwaitProcess> _waiting;

        public const string
            REQUEST = "req",
            RESPONSE = "resp";

        public struct Request {
            public Method Method;
            public long Address;
            public long MsgNo;
            public object Data;
        }

        public Sendy(IMyIntergridCommunicationSystem _IGC) {
            IGC = _IGC;
        }
        
        public abstract void HandleRequest(Request request);
        
        private void Receive() {
            while(IGC.UnicastListener.HasPendingMessage) {
                HandleMessage(IGC.UnicastListener.AcceptMessage());
            }

            while(Broadcast != null && Broadcast.HasPendingMessage) {
                HandleMessage(Broadcast.AcceptMessage());
            }
        }

        private void HandleMessage(MyIGCMessage msg) {
            switch(msg.Tag) {
                case REQUEST: {
                    if(!(msg.Data is MyTuple<Method, long, object>)) { Log.Warn($"Invalid request payload: {msg.Data.GetType()}"); return; }
                    var data = (MyTuple<Method, long, object>)msg.Data;
                    Request req = new Request() {
                        Method = data.Item1,
                        Address = msg.Source,
                        MsgNo = data.Item2,
                        Data = data.Item3,
                    };
                    
                    HandleRequest(req);
                } break;

                case RESPONSE: {
                    if(!(msg.Data is MyTuple<long, object>)) { Log.Warn($"Invalid response payload: {msg.Data.GetType()}"); return; }
                    var data = (MyTuple<long, object>)msg.Data;
                    AwaitProcess proc;
                    if(_waiting.TryGetValue(data.Item1, out proc)) {
                        proc.From = msg.Source;
                        proc.Response = data.Item2;
                    }
                } break;
            }
        }

        abstract class AwaitProcess: Process<object> {
            protected double _timeOut = -1, _startTime;
            protected long _msgNo;
            public object Response = null;
            public long From = -1;
            protected Sendy<Method> _sendy;

            public AwaitProcess(Sendy<Method> sendy, double timeOut, long msgNo) {
                _timeOut = timeOut;
                _msgNo = msgNo;
                _sendy = sendy;
            }

            public override void Begin(double time) {
                _startTime = time;
                base.Begin(time);
            }
        }

        class AwaitResponseProcess: AwaitProcess {
            public AwaitResponseProcess(Sendy<Method> sendy, double timeOut, long msgNo) : base(sendy, timeOut, msgNo) {}

            protected override IEnumerator<object> Run() {
                while(Response == null && _timeOut != -1 && (_time - _startTime) > _timeOut) {
                    yield return null;
                }

                _sendy._waiting.Remove(_msgNo);
                yield return Response;
            } 
        }

        class AwaitResponsesProcess: AwaitProcess {
            Process<MyTuple<long, object>, bool> _recv;
            public AwaitResponsesProcess(
                    Sendy<Method> sendy,
                    double timeOut,
                    long msgNo,
                    Process<MyTuple<long, object>, bool> recv
                ) : base(sendy, timeOut, msgNo) {
                _recv = recv;
            }

            protected override IEnumerator<object> Run() {
                while(_timeOut != -1 && (_time - _startTime) > _timeOut) {
                    if(Response != null) {
                        _recv.Begin(_time, MyTuple.Create(From, Response));
                        while(!_recv.Poll(_time)) { yield return Nil._; }
                        Response = null;
                    }

                    yield return null;
                }
                _sendy._waiting.Remove(_msgNo);
                yield return null;
            }
        }

        public long SendRequest(long addr, Method method, object data = null) {
            long msgNo = _msgNo++;
            IGC.SendUnicastMessage(addr, REQUEST, MyTuple.Create(method, msgNo, data));
            return msgNo;
        }

        public long BroadcastRequest(string tag, Method method, object data = null) {
            long msgNo = _msgNo++;
            IGC.SendBroadcastMessage(tag, MyTuple.Create(method, msgNo, data));
            return msgNo;
        }
        
        public void Respond(Request req, object data = null) {
            IGC.SendUnicastMessage(req.Address, RESPONSE, MyTuple.Create(req.MsgNo, data));
        }

        public void OnResponse(long msg, ReceiveFunc func) => _waiting[msg] = func;

        public Process<object> AwaitResponses(long msg, Func<long, object, bool> process, double timeOut = -1) {
            var proc = new AwaitResponsesProcess(this, timeOut, msg, process);
            _waiting[msg] = (addr, data) => {
                proc.Response = data;
                proc.From = addr;
            };
            return proc;
        }

        public Process<object> AwaitResponse(long msg, double timeOut = -1) {
            var proc = new AwaitResponseProcess(this, timeOut, msg);
            _waiting[msg] = (addr, data) => {
                proc.Response = data;
                proc.From = addr;
            };
            return proc;
        }
    }
}
