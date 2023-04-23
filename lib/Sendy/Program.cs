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
        public readonly IMyIntergridCommunicationSystem IGC;
        long _msgNo = 1;
        Dictionary<long, AwaitResponseProcess> _waiting;

        public struct Request {
            public Method Method;
            public long Address;
            public long MsgNo;
            public object Data;
        }
        
        public abstract void HandleRequest(Request request);

        public class AwaitResponseProcess: Process<object> {
            long _timeOut = -1;
            long _msgNo;
            object _response = null;

            public override IEnumerator<object> Run() {
                while(_response == null) {

                }
            } 
        }

        public void SendRequest<T>(long addr, Method method, T data) {
            IGC.SendUnicastMessage(addr, "req", MyTuple.Create(method, _msgNo++, data));
        }
    }
}
