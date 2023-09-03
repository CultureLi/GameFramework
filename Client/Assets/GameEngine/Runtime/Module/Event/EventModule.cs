using System;
using System.Collections.Generic;
using GameEngine.Runtime.Module;
using GameEngine.Runtime.Base.ReferencePool;

namespace GameEngine.Runtime.Module.Event
{
    public class EventModule:ModuleBase
    {
        public override int Priority => 0;

        private EventMgr eventMgr;

        public EventModule()
        {
            eventMgr = new EventMgr();
        }

        public override void OnInit(InitModuleParamBase param = null)
        {
            eventMgr.Init();
        }

        /// <summary>
        /// 监听事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        public void AddListener<T>(Action<T> handler) where T : EventBase, IReference, new()
        {
            eventMgr.AddListener(handler);
        }

        /// <summary>
        /// 移除监听
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        public void RemoveListener<T>(Action<T> handler) where T : EventBase, IReference, new()
        {           
            eventMgr.RemoveListener(handler);
        }

        /// <summary>
        /// 广播事件，立刻
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        public void BroadCast<T>(T e) where T : EventBase, IReference, new()
        {
            eventMgr.BroadCast(e);
        }

        public void BroadCast<T>(Action<T> initFun) where T : EventBase, IReference, new()
        {
            eventMgr.BroadCast(initFun);
        }

        /// <summary>
        /// 广播事件，下一帧
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        public void BroadCastAsync<T>(T e) where T : EventBase, IReference, new()
        {
            eventMgr.BroadCastAsync(e);
        }

        public void BroadCastAsync<T>(Action<T> initFun) where T : EventBase, IReference, new()
        {
            eventMgr.BroadCastAsync(initFun);
        }
       
        public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            eventMgr.OnUpdate(elapseSeconds, realElapseSeconds);
        }

        public override void OnFixUpdate(float elapseSeconds, float realElapseSeconds)
        {
            
        }

        public override void OnLateUpdate(float elapseSeconds, float realElapseSeconds)
        {
        }

        public override void Release()
        {
            eventMgr.Release();
        }

    }
}
