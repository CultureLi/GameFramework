using System;
using System.Collections.Generic;

public class EventManager : Singleton<EventManager>
{
    // 多播委托存储
    private Dictionary<Type, Delegate> eventTable = new Dictionary<Type, Delegate>();

    /// <summary>
    /// 注册普通监听器
    /// </summary>
    public void Subscribe<T>(Action<T> listener) where T : IEvent
    {
        var type = typeof(T);
        if (eventTable.ContainsKey(type))
            eventTable[type] = Delegate.Combine(eventTable[type], listener);
        else
            eventTable[type] = listener;

        Log($"[EventManager] Subscribed to {type.Name}");
    }


    /// <summary>
    /// 注销监听器
    /// </summary>
    public void Unsubscribe<T>(Action<T> listener) where T : IEvent
    {
        var type = typeof(T);
        if (eventTable.ContainsKey(type))
        {
            eventTable[type] = Delegate.Remove(eventTable[type], listener);
            if (eventTable[type] == null)
                eventTable.Remove(type);
        }

        Log($"[EventManager] Unsubscribed from {type.Name}");
    }

    /// <summary>
    /// 派发事件
    /// </summary>
    public void Dispatch<T>(T evt) where T : IEvent
    {
        var type = typeof(T);
        Log($"[EventManager] Dispatching {type.Name}");

        if (eventTable.TryGetValue(type, out var del))
        {
            if (del is Action<T> callback)
            {
                callback.Invoke(evt);
            }
        }

    }

    private void Log(string message)
    {
#if DEBUG || UNITY_EDITOR
        Console.WriteLine(message); // 替换成 Debug.Log(message) 适配 Unity
#endif
    }
}
