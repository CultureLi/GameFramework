using System;
using System.Collections.Generic;

namespace Framework
{
    public interface IActionList<T>
    {
        void Invoke(T ev);
    }

    /// <summary>
    /// Invoke可能传入的是基类对象，需要在回调中转成基类类型
    /// </summary>
    /// <typeparam name="T1"> 注册回调中参数类型</typeparam>
    /// <typeparam name="T2"> 参数基类类型</typeparam>
    public class ActionList<T1,T2> : IActionList<T2> where T1 : T2
    {
        private List<Action<T1>> _callback = new List<Action<T1>>();

        public void AddListener(Action<T1> cb)
        {
            if (!_callback.Contains(cb))
                _callback.Add(cb);
        }

        public void RemoveListener(Action<T1> cb)
        {
            _callback.Remove(cb);
        }

        public int Count => _callback.Count;

        public void Invoke(T2 ev)
        {
            foreach (Action<T1> cb in _callback)
            {
                try
                {
                    cb?.Invoke((T1)ev);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }
        }
    }

    /// <summary>
    /// 回调中传入参数的跟实际是同种类型
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ActionList<T> : IActionList<T>
    {
        private List<Action<T>> _callback = new List<Action<T>>();

        public void Add(Action<T> cb)
        {
            if (!_callback.Contains(cb))
                _callback.Add(cb);
        }

        public void Remove(Action<T> cb)
        {
            _callback.Remove(cb);
        }

        public int Count => _callback.Count;

        public void Invoke(T ev)
        {
            foreach (Action<T> cb in _callback)
            {
                try
                {
                    cb?.Invoke(ev);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }
        }
    }
}
