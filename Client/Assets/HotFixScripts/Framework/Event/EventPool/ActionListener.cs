using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// Action 监听器
    /// </summary>
    /// <typeparam name="T">事件类型。</typeparam>
    public sealed partial class ActionListener<TArg> where TArg : ArgBase
    {
        private readonly Dictionary<Type, IActionList<TArg>> _listeners = new();

        private readonly Queue<ArgNode> _eventQueue = new();

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
        public void Subscribe<T>(Action<T> listener) where T : TArg
        {
            var eventType = typeof(T);
            if (!_listeners.ContainsKey(eventType))
                _listeners[eventType] = new ActionList<T, TArg>();

            if (!_listeners.TryGetValue(eventType, out var eventListener))
            {
                eventListener = new ActionList<T, TArg>();
                _listeners[eventType] = eventListener;
            }

            (eventListener as ActionList<T, TArg>).AddListener(listener);
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="listener"></param>
        public void Unsubscribe<T>(Action<T> listener) where T : TArg
        {
            var eventType = typeof(T);
            if (_listeners.ContainsKey(eventType))
            {
                var eventListener = (_listeners[eventType] as ActionList<T, TArg>);
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
        public void Broadcast<T>(T ev = null) where T : TArg
        {
            var eventType = typeof(T);
            Broadcast(eventType, ev);
        }

        public void Broadcast(Type eventType, TArg ev)
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
        public void BroadcastAsync<T>(T ev = null) where T : TArg
        {
            var data = ArgNode.Create(ev);
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
