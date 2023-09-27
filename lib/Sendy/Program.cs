using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System;
using VRage;

namespace IngameScript {
    /// <summary>
    /// General purpose unicast communications protocol with boilerplate
    /// reducing methods to register actions for specific requests
    /// </summary>
    public abstract class Sendy<Method> {
        public readonly IMyIntergridCommunicationSystem IGC;
        public IMyBroadcastListener Broadcast = null;

        long _msgNo = 1;
        Dictionary<long, Nullable<Response>> _waiting;

        public const string
            REQUEST = "req",
            RESPONSE = "resp";

        public struct Response {
            public long Address;
            public object Data;
        }

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
            yield return Yield.Continue;
                    if(!(msg.Data is MyTuple<long, object>)) { Log.Warn($"Invalid response payload: {msg.Data.GetType()}"); return; }
                    var data = (MyTuple<long, object>)msg.Data;
                    Nullable<Response> resp;
                    if(_waiting.TryGetValue(data.Item1, out resp)) {
                        resp = new Response() {
                            Address = msg.Source,
                            Data = data.Item2
                        };
                    }
                } break;
            }
        }

        public IEnumerable<Nullable<Response>> WaitResponse(long msgNo, double timeOut = -1) {
            var startTime = Process.Time;
            while(timeOut != -1 && Process.Time - startTime < timeOut) {
                if(_waiting[msgNo].HasValue) {
                    yield return _waiting[msgNo];
                    yield break;
                }

                yield return null;
            }

            _waiting.Remove(msgNo);
        }

        public IEnumerable<Nullable<Response>> WaitResponses(long msgNo, double timeOut = -1) {
            var startTime = Process.Time;
            while(timeOut != -1 && (Process.Time - startTime) < timeOut) {
                yield return _waiting[msgNo].Value;
                _waiting[msgNo] = null;
            }

            _waiting.Remove(msgNo);
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
    }
}
