using System;
using System.Collections.Generic;

namespace Framework
{
    /// <summary>
    /// Action 监听器
    /// </summary>
    /// <typeparam name="TArg">参数类型。</typeparam>
    public sealed partial class ActionListener<TArg>
    {
        private readonly Dictionary<Type, IActionList<TArg>> _listeners = new();

        private readonly Queue<ArgNode> _nodeQueue = new();

        public int NodeCount
        {
            get
            {
                return _nodeQueue.Count;
            }
        }

        /// <summary>
        /// 添加回调
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="callback"></param>
        public void AddListener<T>(Action<T> callback) where T : TArg
        {
            var type = typeof(T);

            if (!_listeners.TryGetValue(type, out var listener))
            {
                listener = new ActionList<T, TArg>();
                _listeners[type] = listener;
            }

            (listener as ActionList<T, TArg>).AddListener(callback);
        }

        /// <summary>
        /// 移除回调
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="callback"></param>
        public void RemoveListener<T>(Action<T> callback) where T : TArg
        {
            var type = typeof(T);

            if (_listeners.TryGetValue(type, out var value) && value is ActionList<T, TArg> listener)
            {
                listener.RemoveListener(callback);
                if (listener.Count == 0)
                {
                    _listeners.Remove(type);
                }
            }
        }

        /// <summary>
        /// 派发事件（同步）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ev"></param>
        public void Dispatch<T>(T ev = default) where T : TArg
        {
            var eventType = typeof(T);
            Dispatch(eventType, ev);
        }

        public void Dispatch(Type eventType, TArg ev = default)
        {
            if (_listeners.TryGetValue(eventType, out var listener))
            {
                listener.Invoke(ev);
            }
        }

        /// <summary>
        /// 派发事件（异步）放入队列，在Update中处理
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ev"></param>
        public void DispatchAsync<T>(T ev) where T : TArg
        {
            var data = ArgNode.Spawn(ev);
            _nodeQueue.Enqueue(data);
        }

        /// <summary>
        /// 处理异步事件，每帧最多处理50个
        /// </summary>
        private readonly int _maxCntPerFrame = 50;
        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            int msgCount = 0;
            while (_nodeQueue.Count > 0 && msgCount < _maxCntPerFrame)
            {
                msgCount++;
                var node = _nodeQueue.Dequeue();
                Dispatch(node.Type, node.Data);
                ArgNode.UnSpawn(node);
            }
        }

        public void ClearNodes()
        {
            foreach (var node in _nodeQueue)
            {
                ReferencePool.Release(node);
            }
            _nodeQueue.Clear();
        }

        public void Shutdown()
        {
            ClearNodes();
            _listeners.Clear();
        }
    }
}
