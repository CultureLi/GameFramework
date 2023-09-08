using System;
using System.Collections.Generic;
using GameEngine.Runtime.Base;
using GameEngine.Runtime.Base.RefPool;

namespace GameLauncher.Runtime.Event
{
    internal class LauncherEventMgr:Singleton<LauncherEventMgr>
    {
        internal interface IEventHandlers
        {
            public void BroadCast<T>(T e) where T : LauncherEventBase;
        }
        internal class EventHandlers<T> : IEventHandlers where T : LauncherEventBase
        {
            public List<Action<T>> m_Handlers = new();

            public void AddListener(Action<T> handler)
            {
                if (handler == null)
                    return;
                if (m_Handlers.Contains(handler))
                    return;

                m_Handlers.Add(handler);
            }

            public void RemoveListener(Action<T> handler)
            {
                if (handler == null)
                    return;

                m_Handlers.Remove(handler);
            }

            public void BroadCast<T1>(T1 e) where T1 : LauncherEventBase
            {
                foreach (var handler in m_Handlers)
                {
                    handler.Invoke(e as T);
                }
            }

        }

        // 事件类型和处理函数
        private Dictionary<Type, IEventHandlers> m_EventHandlers = new();
        //事件队列
        private Queue<LauncherEventBase> m_EventQueue = new();

        public LauncherEventMgr()
        {
        }

        public void Init()
        {
            m_EventHandlers.Clear();
            m_EventQueue.Clear();
        }

        /// <summary>
        /// 监听事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        public void AddListener<T>(Action<T> handler) where T : LauncherEventBase, IReference, new()
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
        public void RemoveListener<T>(Action<T> handler) where T : LauncherEventBase, IReference, new()
        {
            GetHandlers<T>()?.RemoveListener(handler);
        }

        /// <summary>
        /// 广播事件，立刻
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        public void BroadCast<T>(T e) where T : LauncherEventBase, IReference, new()
        {
            GetHandlers<T>()?.BroadCast(e);
            e.Release();
        }

        public void BroadCast<T>(Action<T> initFun=null) where T : LauncherEventBase, IReference, new()
        {
            var e = LauncherEventBase.Acquire<T>();
            initFun?.Invoke(e);
            BroadCast(e);
            e.Release();
        }

        private void BroadCast(Type type, LauncherEventBase e)
        {
            GetHanlders(type)?.BroadCast(e);
            e.Release();
        }

        /// <summary>
        /// 广播事件，下一帧
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        public void BroadCastAsync<T>(T e) where T : LauncherEventBase, IReference, new()
        {
            m_EventQueue.Enqueue(e);
        }

        public void BroadCastAsync<T>(Action<T> initFun) where T : LauncherEventBase, IReference, new()
        {
            var e = LauncherEventBase.Acquire<T>();
            initFun?.Invoke(e);
            m_EventQueue.Enqueue(e);
        }

        private EventHandlers<T> CreateHandlers<T>() where T : LauncherEventBase, IReference, new()
        {
            var type = typeof(T);
            if (!m_EventHandlers.TryGetValue(type, out var handlers))
            {
                handlers = new EventHandlers<T>();
                m_EventHandlers[type] = handlers;
            }
            return (EventHandlers<T>)handlers;
        }

        private EventHandlers<T> GetHandlers<T>() where T : LauncherEventBase, IReference, new()
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

        public void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            while (m_EventQueue.Count > 0)
            {
                var e = m_EventQueue.Dequeue();
                BroadCast(e.GetType(), e);
            }
        }


        public void Release()
        {
            m_EventHandlers.Clear();
            m_EventQueue.Clear();
        }
    }
}
