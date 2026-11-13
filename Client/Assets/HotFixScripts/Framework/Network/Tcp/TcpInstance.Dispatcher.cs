using Google.Protobuf;
using System;
using System.Collections.Concurrent;
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

            // 收包线程只往这个线程安全队列里投递，主线程 Update 统一派发。
            // 不能直接在收包线程里调用 ActionListener：它内部的监听表/引用池都只在主线程用。
            private readonly ConcurrentQueue<IMessage> _pendingMsgs = new ConcurrentQueue<IMessage>();

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
            /// 收包线程调用：把消息投递给主线程，并立刻归还 SCPacket。
            /// </summary>
            public void DispatchAsync(SCPacket packet)
            {
                if (packet == null)
                {
                    return;
                }

                if (packet.msg != null)
                {
                    _pendingMsgs.Enqueue(packet.msg);
                }

                // SCPacket 的生命周期到此结束，消息本身已被队列持有。
                ReferencePool.Release(packet);
            }

            public void Update(float elapseSeconds, float realElapseSeconds)
            {
                // 主线程：按消息的真实类型派发（监听器注册的是具体消息类型）。
                while (_pendingMsgs.TryDequeue(out var msg))
                {
                    _ActionListener.Dispatch(msg.GetType(), msg);
                }

                _ActionListener.Update(elapseSeconds, realElapseSeconds);
            }

            public void Dispose()
            {
                while (_pendingMsgs.TryDequeue(out _))
                {
                }
                _ActionListener.Dispose();
            }
        }
    }
}
