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

            private ActionListener<IMessage> _ActionListener = new ActionListener<IMessage>();

            /// <summary>
            /// 注册回调函数
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="handler"></param>
            public void RegisterMsg<T>(Action<T> handler) where T : IMessage
            {
                _ActionListener.AddListener(handler);
            }

            /// <summary>
            /// 反注册回调函数
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="handler"></param>
            public void UnregisterMsg<T>(Action<T> handler) where T : IMessage
            {
                _ActionListener.RemoveListener(handler);
            }

            /// <summary>
            /// 异步派发消息
            /// </summary>
            /// <param name="packet"></param>
            public void DispatchAsync(SCPacket packet)
            {
                var type = ProtoTypeHelper.GetMsgType(packet.id);
                _ActionListener.DispatchAsync(packet.msg);

            }

            public void Update(float elapseSeconds, float realElapseSeconds)
            {
                _ActionListener.Update(elapseSeconds, realElapseSeconds);
            }

            public void Dispose()
            {
                _ActionListener.Shutdown();
            }
        }
    }
}
