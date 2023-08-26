using System;
using System.Collections.Generic;

namespace GameEngine.Runtime.Module.Event
{
    public interface IEventHandlers
    {
        public void BroadCast<T>(T e) where T : EventBase;
    }
    public class EventHandlers<T> : IEventHandlers where T : EventBase
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

        public void BroadCast<T1>(T1 e) where T1 : EventBase
        {
            foreach (var handler in m_Handlers)
            {
                handler.Invoke(e as T);
            }
        }
   
    }
}
