using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    internal class TimerMgr : ITimerMgr, IFramework
    {
        //timer map
        private Dictionary<int, Timer> _timers = new Dictionary<int, Timer>();
        //预添加和预删除，防止直接在_timers上操作,造成迭代器失效
        private List<Timer> _preAdd = new List<Timer>();
        private List<int> _preRemove = new List<int>();

        /// <summary>
        /// 添加计时器
        /// </summary>
        /// <param name="interval">触发间隔单位（秒）</param>
        /// <param name="callback">回调函数</param>
        /// <param name="duration">持续时长：小于0无限时长，等于0只触发一次，大于0持续N秒</param>
        /// <returns></returns>
        public int AddTimer(float interval, Action<TimerContext> callback, float duration = 0f)
        {
            var timer = ReferencePool.Acquire<Timer>();
            timer.Clear();
            timer.id = GenUid();
            timer.interval = interval;
            timer.callback = callback;
            timer.duration = duration;
            if (Mathf.Approximately(duration, 0f))
                timer.duration = interval;

            _preAdd.Add(timer);
            return timer.id;
        }

        public void RemoveTimer(int id)
        {
            if (_timers.TryGetValue(id, out Timer timer))
            {
                // 已经在待删除队列里的重复调用会在这里再次入队，DoPreRemove 里做了幂等处理。
                timer.MarkDel();
                _preRemove.Add(id);
                return;
            }

            // 同一帧内"添加后立刻删除"：此时 timer 还在 _preAdd 里，还没进入 _timers，
            // 上面的 TryGetValue 找不到。直接在预添加队列里摘掉并回收，否则它会照常被加进去并触发。
            for (int i = _preAdd.Count - 1; i >= 0; i--)
            {
                if (_preAdd[i].id != id)
                {
                    continue;
                }

                ReferencePool.Release(_preAdd[i]);
                _preAdd.RemoveAt(i);
                return;
            }
        }

        private int _UID = 1;
        private int GenUid()
        {
            if (_UID == int.MaxValue)
                _UID = 1;
            return _UID++;
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            DoPreAdd();

            foreach ((var id, var timer) in _timers)
            {
                var stop = timer.Tick(realElapseSeconds);
                if (stop)
                {
                    RemoveTimer(id);
                }
            }

            DoPreRemove();
        }

        public void Release()
        {
            foreach (var timer in _preAdd)
            {
                ReferencePool.Release(timer);
            }
            _preAdd.Clear();

            _preRemove.Clear();

            foreach ((var _, var timer) in _timers)
            {
                ReferencePool.Release(timer);
            }

            _timers.Clear();
        }

        public void Shutdown(EShutdownType type)
        {
            Release();
        }


        private void DoPreAdd()
        {
            if (_preAdd.Count == 0)
                return;

            foreach (var timer in _preAdd)
            {
                // id 回绕后理论上可能撞上未回收的 timer，这里不覆盖已有 timer，直接回收新的。
                if (_timers.ContainsKey(timer.id))
                {
                    UnityEngine.Debug.LogError($"Timer id '{timer.id}' is already in use.");
                    ReferencePool.Release(timer);
                    continue;
                }

                _timers.Add(timer.id, timer);
            }

            _preAdd.Clear();
        }

        private void DoPreRemove()
        {
            if (_preRemove.Count == 0)
                return;

            foreach (var id in _preRemove)
            {
                if (!_timers.TryGetValue(id, out var timer))
                    continue;

                ReferencePool.Release(timer);
                _timers.Remove(id);
            }
            _preRemove.Clear();
        }
    }
}
