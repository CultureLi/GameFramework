using System;
using System.Collections.Generic;
using GameEngine.Runtime.Pool.ReferencePool;

namespace GameEngine.Runtime.Event
{
    public class EventManager
    {
        public int Priority
        {
            get
            {
                return 100;
            }
        }
        // 事件类型和处理函数
        private Dictionary<Type, IEventHandlers> m_EventHandlers = new();
        //事件队列
        private Queue<EventBase> m_EventQueue = new();

        public void Init(object[] args)
        {
            m_EventHandlers.Clear();
            m_EventQueue.Clear();
        }

        /// <summary>
        /// 监听事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        public void AddListener<T>(Action<T> handler) where T : EventBase, IReference, new()
        {
            var handlers = GetHandlers<T>();
            if (handlers == null)
                handlers = CreateHandlers<T>();

            handlers.AddListener(handler);
        }

        /// <summary>
        /// 移除监听
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        public void RemoveListener<T>(Action<T> handler) where T : EventBase, IReference, new()
        {           
            GetHandlers<T>()?.RemoveListener(handler);
        }

        /// <summary>
        /// 广播事件，立刻
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        public void BroadCast<T>(T e) where T : EventBase, IReference, new()
        {
            GetHandlers<T>()?.BroadCast(e);
            e.Release();
        }

        public void BroadCast<T>(Action<T> initFun) where T : EventBase, IReference, new()
        {
            var e = EventBase.Acquire<T>();
            initFun?.Invoke(e);
            BroadCast(e);
            e.Release();
        }

        private void BroadCast(Type type, EventBase e)
        {
            GetHanlders(type)?.BroadCast(e);
            e.Release();
        }

        /// <summary>
        /// 广播事件，下一帧
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        public void BroadCastAsync<T>(T e) where T : EventBase, IReference, new()
        {
            m_EventQueue.Enqueue(e);
        }

        public void BroadCastAsync<T>(Action<T> initFun) where T : EventBase, IReference, new()
        {
            var e = EventBase.Acquire<T>();
            initFun?.Invoke(e);
            m_EventQueue.Enqueue(e);
        }

        private EventHandlers<T> CreateHandlers<T>() where T : EventBase, IReference, new()
        {
            var type = typeof(T);
            if (!m_EventHandlers.TryGetValue(type, out var handlers))
            {
                handlers = new EventHandlers<T>();
                m_EventHandlers[type] = handlers;
            }
            return (EventHandlers<T>)handlers;
        }

        private EventHandlers<T> GetHandlers<T>() where T : EventBase, IReference, new()
        {
            var type = typeof(T);
            m_EventHandlers.TryGetValue(type, out var handlers);
            return (EventHandlers<T>)handlers;
        }

        private IEventHandlers GetHanlders(Type type)
        {
            m_EventHandlers.TryGetValue(type, out var handlers);
            return handlers;
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            while(m_EventQueue.Count > 0)
            {
                var e = m_EventQueue.Dequeue();
                BroadCast(e.GetType(),e);
            }
        }

        public void Release()
        {
            m_EventHandlers.Clear();
            m_EventQueue.Clear();
        }
    }
}
