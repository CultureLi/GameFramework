using GameEngine.Runtime.Module;
using System;
using System.Collections.Generic;

namespace GameEngine.Runtime.Module.Timer
{
    public class TimerModule:ModuleBase
    {
        public override int Priority => 0;
        private TimerMgr timerMgr = new();
        /// <summary>
        /// 创建timer
        /// </summary>
        /// <param name="interval">触发间隔 单位ms</param>
        /// <param name="completedCb">完成回调</param>
        /// <param name="repeatCnt">重复次数</param>       
        /// <param name="userData">用户数据</param>
        /// <returns></returns>
        public int CreateTimer(int interval, Action<object> completedCb, int repeatCnt = 1, object userData = null)
        {
            return timerMgr.CreateTimer(interval, repeatCnt, userData);
        }

        /// <summary>
        /// 删除timer
        /// </summary>
        /// <param name="timerId"></param>
        public void DeleteTimer(int timerId)
        {
            timerMgr.DeleteTimer(timerId);
        }

        /// <summary>
        /// 暂停
        /// </summary>
        public void Pause()
        {
            timerMgr.Pause();
        }

        /// <summary>
        /// 唤醒
        /// </summary>
        public void Resume()
        {
            timerMgr.Resume();
        }

        /// <summary>
        /// 更新
        /// </summary>
        public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            timerMgr.OnUpdate(elapseSeconds, realElapseSeconds);
        }

        public override void Release()
        {
            timerMgr.Release();
        }

        public override void OnFixUpdate(float elapseSeconds, float realElapseSeconds)
        {
        }

        public override void OnLateUpdate(float elapseSeconds, float realElapseSeconds)
        {
        }

       
    }
}
