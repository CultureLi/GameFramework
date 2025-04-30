using System;
using System.Collections.Generic;

namespace Framework
{
    internal sealed class EventMgr : IEventMgr, IFrameworkModule
    {
        // 多播委托存储
        private Dictionary<Type, List<Delegate>> eventTable = new Dictionary<Type, List<Delegate>>();

        /// <summary>
        /// 注册普通监听器
        /// </summary>
        public void Subscribe<T>(Action<T> listener) where T : EventBase
        {
            var type = typeof(T);
            if (!eventTable.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                eventTable[type] = list;
            }
            
            if (!list.Contains(listener))
                list.Add(listener);
        }


        /// <summary>
        /// 注销监听器
        /// </summary>
        public void Unsubscribe<T>(Action<T> listener) where T : EventBase
        {
            var type = typeof(T);
            if (eventTable.TryGetValue(type, out var list))
            {
               list.Remove(listener);
            }
        }

        /// <summary>
        /// 触发事件
        /// </summary>
        public void Fire<T>(T evt = null) where T : EventBase
        {
            var type = typeof(T);
            if (eventTable.TryGetValue(type, out var list))
            {
                foreach (var callback in list)
                {
                    callback.DynamicInvoke(evt);
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