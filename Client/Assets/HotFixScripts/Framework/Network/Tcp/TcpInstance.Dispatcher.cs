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
        /// <summary>
        /// 消息回调函数派发器
        /// </summary>
        private sealed class Dispatcher
        {
            Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();

            /// <summary>
            /// 注册回调函数
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="handler"></param>
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

            /// <summary>
            /// 反注册回调函数
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="handler"></param>
            public void UnregisterMsg<T>(Action<T> handler) where T : IMessage
            {
                var msgType = typeof(T);
                if (_handlers.TryGetValue(msgType, out var list))
                {
                    list.Remove(handler);
                }
            }

            /// <summary>
            /// 派发给消息回调函数
            /// </summary>
            /// <param name="packet"></param>
            public void DispatchMsg(SCPacket packet)
            {
                var type = ProtoTypeHelper.GetMsgType(packet.id);
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
