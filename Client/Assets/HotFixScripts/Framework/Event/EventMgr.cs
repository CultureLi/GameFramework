using System;

namespace Framework
{
    internal sealed class EventMgr : IEventMgr, IFramework
    {
        private ActionListener<EventBase> _ActionListener;

        public EventMgr()
        {
            _ActionListener = new ActionListener<EventBase>();
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="listener"></param>
        public void Subscribe<T>(Action<T> listener) where T : EventBase
        {
            _ActionListener.Subscribe(listener);
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="listener"></param>
        public void Unsubscribe<T>(Action<T> listener) where T : EventBase
        {
            _ActionListener.Unsubscribe(listener);
        }

        /// <summary>
        /// 同步广播，立即调用
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ev"></param>
        public void Broadcast<T>(T ev = null) where T : EventBase
        {
            _ActionListener.Broadcast(ev);
        }

        /// <summary>
        /// 异步广播，放入队列，等待 Update 中派发
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ev"></param>
        public void BroadcastAsync<T>(T ev = null) where T : EventBase
        {
            _ActionListener.BroadcastAsync(ev);
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            _ActionListener.Update(elapseSeconds, realElapseSeconds);
        }

        public void Shutdown()
        {
            _ActionListener?.Shutdown();
        }
    }
}