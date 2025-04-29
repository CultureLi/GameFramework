using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    public class EventMgr : IEventMgr, IFrameworkModule
    {
        // 多播委托存储
        private Dictionary<Type, Delegate> eventTable = new Dictionary<Type, Delegate>();

        /// <summary>
        /// 注册普通监听器
        /// </summary>
        public void Subscribe<T>(Action<T> listener) where T : EventBase
        {
            var type = typeof(T);
            if (eventTable.ContainsKey(type))
                eventTable[type] = Delegate.Combine(eventTable[type], listener);
            else
                eventTable[type] = listener;

            Debug.Log($"[EventManager] Subscribed to {type.Name}");
        }


        /// <summary>
        /// 注销监听器
        /// </summary>
        public void Unsubscribe<T>(Action<T> listener) where T : EventBase
        {
            var type = typeof(T);
            if (eventTable.ContainsKey(type))
            {
                eventTable[type] = Delegate.Remove(eventTable[type], listener);
                if (eventTable[type] == null)
                    eventTable.Remove(type);
            }

            Debug.Log($"[EventManager] Unsubscribed from {type.Name}");
        }

        /// <summary>
        /// 触发事件
        /// </summary>
        public void Fire<T>(T evt = null) where T : EventBase
        {
            var type = typeof(T);
            Debug.Log($"[EventManager] Dispatching {type.Name}");

            if (eventTable.TryGetValue(type, out var del))
            {
                if (del is Action<T> callback)
                {
                    callback.Invoke(evt);
                }
            }

        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            
        }

        public void Shutdown()
        {
            eventTable.Clear();
        }
    }
}