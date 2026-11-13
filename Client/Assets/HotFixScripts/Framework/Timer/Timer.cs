using System;

namespace Framework
{
    //timer上下文
    public class TimerContext : IReference
    {
        public int triggeredCnt;
        public float totalDuration;
        public float curDuration;

        public void Clear()
        {
            triggeredCnt = 0;
            totalDuration = 0;
            curDuration = 0;
        }
    }

    //timer
    public class Timer : IReference
    {
        public int id;
        //触发间隔
        public float interval;
        //持续时长
        public float duration;
        //触发回调
        public Action<TimerContext> callback;
        //是否预删除
        private bool _isPreDel;

        private float _interval = 0;
        private float _duration = 0;
        //已经触发的次数
        private int _triggeredCnt = 0;

        public void MarkDel()
        {
            _isPreDel = true;
        }
        public bool Tick(float deltaTime)
        {
            if (_isPreDel)
                return false;

            _interval += deltaTime;
            _duration += deltaTime;

            // interval <= 0 视为"每帧触发"，不做累加，避免减法后仍是负数导致行为不确定。
            if (interval <= 0f || _interval >= interval)
            {
                if (interval > 0f)
                {
                    // 保留余数而不是清零：长时间运行的循环定时器不会因为每帧截断而持续漂移。
                    _interval -= interval;
                }
                else
                {
                    _interval = 0f;
                }

                var context = ReferencePool.Acquire<TimerContext>();
                context.Clear();
                _triggeredCnt++;
                context.triggeredCnt = _triggeredCnt;
                context.totalDuration = duration;
                context.curDuration = _duration;

                // 单个 timer 的异常不能中断 TimerMgr 的整个 Update。
                try
                {
                    callback?.Invoke(context);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }

                ReferencePool.Release(context);
            }

            if (duration < 0)
                return false;

            if (_duration >= duration)
                return true;

            return false;
        }

        public void Clear()
        {
            id = 0;
            interval = 0;
            duration = 0;
            callback = null;
            _isPreDel = false;
            _interval = 0;
            _duration = 0;
            _triggeredCnt = 0;
        }
    }
}
