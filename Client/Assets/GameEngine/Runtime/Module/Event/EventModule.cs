using GameEngine.Runtime.Event;
using GameEngine.Runtime.Pool.ReferencePool;
using System;
namespace GameEngine.Runtime.Module.Event
{
    public class EventModule : ModuleBase
    {
        public override int Priority => 0;

        private EventManager module;
        void Awake()
        {
            module = new EventManager();
        }

        public void AddListener<T>(Action<T> handler) where T : EventBase, IReference, new()
        {
            module.AddListener(handler);
        }

        public void RemoveListener<T>(Action<T> handler) where T : EventBase, IReference, new()
        {
            module.RemoveListener(handler);
        }

        public void BroadCast<T>(T e) where T : EventBase, IReference, new()
        {
            module.BroadCast(e);
        }

        public void BroadCast<T>(Action<T> initFun) where T : EventBase, IReference, new()
        { 
            module.BroadCast(initFun); 
        }

        public void BroadCastAsync<T>(T e) where T : EventBase, IReference, new()
        {
            module.BroadCastAsync(e);
        }

        public void BroadCastAsync<T>(Action<T> initFun) where T : EventBase, IReference, new()
        {
            module.BroadCastAsync(initFun);
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
