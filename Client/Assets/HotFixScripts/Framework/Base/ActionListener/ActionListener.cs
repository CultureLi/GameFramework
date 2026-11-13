using System;
using System.Collections.Concurrent;
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

        // 网络收包线程会往里塞，主线程 Update 里取，必须用线程安全队列。
        private readonly ConcurrentQueue<ArgNode> _nodeQueue = new();

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
        /// 派发事件（异步）放入队列，在Update中处理。
        /// 使用编译期类型 typeof(T) 作为派发键，与同步 Dispatch&lt;T&gt; 保持一致
        /// （原来用 ev.GetType()，同一个 Broadcast 走同步/异步会命中不同的监听器）。
        /// </summary>
        public void DispatchAsync<T>(T ev) where T : TArg
        {
            DispatchAsync(typeof(T), ev);
        }

        /// <summary>
        /// 派发事件（异步），按指定类型派发。
        /// 网络层拿到的是 IMessage 运行时对象，需要按它的真实类型派发。
        /// </summary>
        public void DispatchAsync(Type eventType, TArg ev)
        {
            if (eventType == null)
            {
                throw new ArgumentNullException(nameof(eventType));
            }

            _nodeQueue.Enqueue(ArgNode.Spawn(eventType, ev));
        }

        /// <summary>
        /// 处理异步事件，每帧最多处理50个
        /// </summary>
        private readonly int _maxCntPerFrame = 50;
        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            int msgCount = 0;
            while (msgCount < _maxCntPerFrame && _nodeQueue.TryDequeue(out var node))
            {
                msgCount++;
                Dispatch(node.Type, node.Data);
                ArgNode.UnSpawn(node);
            }
        }

        public void ClearNodes()
        {
            while (_nodeQueue.TryDequeue(out var node))
            {
                ArgNode.UnSpawn(node);
            }
        }

        public void Dispose()
        {
            ClearNodes();
            _listeners.Clear();
        }
    }
}
