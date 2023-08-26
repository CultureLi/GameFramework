using GameEngine.Runtime.Pool.ReferencePool;
using GameEngine.Runtime.Timer;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine.Runtime.Module.Timer
{
    public class TimerModule : ModuleBase
    {
        public override int Priority => 0;

        private TimerManager module;
        void Awake()
        {
            module = new TimerManager();
        }

        public int CreateTimer(int interval, Action<object> completedCb, int repeatCnt = 1, object userData = null)
        {
            return module.CreateTimer(interval,completedCb,repeatCnt,userData);
        }

        public void DeleteTimer(int timerId)
        {
            module.DeleteTimer(timerId);
        }

        public void Pause()
        {
            module.Pause();
        }

        public void Resume()
        {
            module.Resume();
        }

        // Update is called once per frame
        public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            module.Update(elapseSeconds, realElapseSeconds);
        }
        public override void OnFixUpdate(float elapseSeconds, float realElapseSeconds)
        {
        }
        public override void OnLateUpdate(float elapseSeconds, float realElapseSeconds)
        {
        }
        /// <summary>
        /// 关闭并清理游戏框架模块。
        /// </summary>
        public override void Release()
        {
            module.Release();
        }
    }
}
