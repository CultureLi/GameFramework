using Framework;
using UnityEngine;

namespace GameEntry
{
    public class FW : MonoBehaviour
    {
        public static IEventMgr EventMgr { get; private set; }
        public static IFsmMgr FsmMgr { get; private set; }
        public static IResourceMgr ResourceMgr { get; private set; }
        public static ITimerMgr TimerMgr { get; private set; }
        public static INetworkMgr NetMgr { get; private set; }
        public static IObjectPoolManager ObjectPoolMgr { get; private set; }
        public static IUIManager UIMgr { get; private set; }


        void Awake()
        {
            InitModules();
        }

        void InitModules()
        {
            EventMgr = FrameworkMgr.GetModule<IEventMgr>();
            FsmMgr = FrameworkMgr.GetModule<IFsmMgr>();
            ResourceMgr = FrameworkMgr.GetModule<IResourceMgr>();
            TimerMgr = FrameworkMgr.GetModule<ITimerMgr>();
            NetMgr = FrameworkMgr.GetModule<INetworkMgr>();
            ObjectPoolMgr = FrameworkMgr.GetModule<IObjectPoolManager>();
            UIMgr = FrameworkMgr.GetModule<IUIManager>();
            UIMgr.SetResourceManager(ResourceMgr);
        }

        void Update()
        {
            FrameworkMgr.Update(Time.deltaTime, Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            FrameworkMgr.Shutdown();
        }

    }
}
