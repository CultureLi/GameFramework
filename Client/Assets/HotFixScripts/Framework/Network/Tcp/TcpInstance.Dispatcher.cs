using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework
{
    public partial class TcpInstance
    {
        private sealed class Dispatcher
        {
            Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();

            public void RegisterMsg<T>(Action<T> handler) where T : IMessage
            {
                var msgType = typeof(T);
                if (!_handlers.TryGetValue(msgType, out var list))
                {
                    list = new List<Delegate>();
                    _handlers[msgType] = list;
                }
                if (!list.Contains(handler))
                    list.Add(handler);
            }

            public void UnregisterMsg<T>(Action<T> handler) where T : IMessage
            {
                var msgType = typeof(T);
                if (_handlers.TryGetValue(msgType, out var list))
                {
                    list.Remove(handler);
                }
            }

            public void DispatchMsg(SCPacket packet)
            {
                var type = MsgTypeIdUtility.GetMsgType(packet.id);
                if (_handlers.TryGetValue(type, out var list))
                {
                    foreach (var handler in list)
                    {
                        handler.DynamicInvoke(packet.msg);
                    }
                }
            }

            public void Dispose()
            {
                _handlers.Clear();
            }
        }
    }
}
