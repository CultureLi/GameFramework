using System;
using System.Collections.Generic;

namespace Framework
{
    internal sealed class EventMgr : IEventMgr, IFramework
    {
        private readonly Dictionary<Type, List<Delegate>> _listeners = new();

        class EventData : IReference
        {
            public Type type;
            public EventBase data;

            public void Reset()
            {
                type = null;
                data = null;
            }
        }
        private readonly Queue<EventData> _eventQueue = new();

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="listener"></param>
        public void Subscribe<T>(Action<T> listener) where T : EventBase
        {
            var eventType = typeof(T);
            if (!_listeners.ContainsKey(eventType))
                _listeners[eventType] = new List<Delegate>();

            _listeners[eventType].Add(listener);
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="listener"></param>
        public void Unsubscribe<T>(Action<T> listener) where T : EventBase
        {
            var eventType = typeof(T);
            if (_listeners.ContainsKey(eventType))
            {
                _listeners[eventType].Remove(listener);
            }
        }

        /// <summary>
        /// 同步广播，立即调用
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ev"></param>
        public void Fire<T>(T ev = null) where T : EventBase
        {
            var eventType = typeof(T);
            if (_listeners.TryGetValue(eventType, out var list))
            {
                foreach (var listener in list)
                {
                    (listener as Action<T>)?.Invoke(ev);
                }
            }
        }

        /// <summary>
        /// 异步广播，放入队列，等待 Update 中派发
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ev"></param>
        public void FireAsync<T>(T ev = null) where T : EventBase
        {
            var data = ReferencePool.Acquire<EventData>();
            data.Reset();
            data.type = typeof(T);
            data.data = ev;
            _eventQueue.Enqueue(data);
        }


        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            while (_eventQueue.Count > 0)
            {
                var ev = _eventQueue.Dequeue();
                var eventType = ev.type;

                if (_listeners.TryGetValue(eventType, out var list))
                {
                    foreach (var listener in list)
                    {
                        listener.DynamicInvoke(ev.data);
                        ReferencePool.Release(ev);
                    }
                }
            }
        }

        public void Shutdown()
        {
            _listeners.Clear();
            ReferencePool.RemoveAll<EventData>();
        }

    }
}