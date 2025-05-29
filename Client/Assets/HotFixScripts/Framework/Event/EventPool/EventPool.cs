using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 事件池。
    /// </summary>
    /// <typeparam name="T">事件类型。</typeparam>
    public sealed partial class EventPool
    {
        private readonly Dictionary<Type, IActionList<EventBase>> _listeners = new();

        private readonly Queue<EventNode> _eventQueue = new();

        /// <summary>
        /// 获取事件数量。
        /// </summary>
        public int EventCount
        {
            get
            {
                return _eventQueue.Count;
            }
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="listener"></param>
        public void Subscribe<T>(Action<T> listener) where T : EventBase
        {
            var eventType = typeof(T);
            if (!_listeners.ContainsKey(eventType))
                _listeners[eventType] = new ActionList<T,EventBase>();

            if (!_listeners.TryGetValue(eventType, out var eventListener))
            {
                eventListener = new ActionList<T,EventBase>();
                _listeners[eventType] = eventListener;
            }

            (eventListener as ActionList<T, EventBase>).AddListener(listener);
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
                var eventListener = (_listeners[eventType] as ActionList<T, EventBase>);
                eventListener.RemoveListener(listener);
                if (eventListener.Count == 0)
                {
                    _listeners.Remove(eventType);
                }
            }
        }

        /// <summary>
        /// 同步广播，立即调用
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ev"></param>
        public void Broadcast<T>(T ev = null) where T : EventBase
        {
            var eventType = typeof(T);
            Broadcast(eventType, ev);
        }

        public void Broadcast(Type eventType, EventBase ev)
        {
            if (_listeners.TryGetValue(eventType, out var listener))
            {
                listener.Invoke(ev);
            }
        }

        /// <summary>
        /// 异步广播，放入队列，等待 Update 中派发
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ev"></param>
        public void BroadcastAsync<T>(T ev = null) where T : EventBase
        {
            var data = EventNode.Create(ev);
            _eventQueue.Enqueue(data);
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            while (_eventQueue.Count > 0)
            {
                var node = _eventQueue.Dequeue();
                Broadcast(node.Type, node.Data);
                ReferencePool.Release(node);
            }
        }

        public void Clear()
        {
            foreach (var node in _eventQueue)
            {
                ReferencePool.Release(node);
            }
            _eventQueue.Clear();
        }

        public void Shutdown()
        {
            Clear();
            _listeners.Clear();
        }
    }
}
