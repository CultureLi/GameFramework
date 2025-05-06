using System;

namespace Framework
{
    public interface ITimerMgr
    {
        int AddTimer(float interval, Action<TimerContext> callback, float duration = 0f);
        void RemoveTimer(int id);
        void Release();
    }
}
