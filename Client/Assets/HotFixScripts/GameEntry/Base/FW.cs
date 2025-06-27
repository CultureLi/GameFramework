using Framework;
using UnityEngine;

namespace GameEntry
{
    public class FW : SingletonMono<FW>
    {
        public static IEventMgr EventMgr { get; private set; }
        public static IFsmMgr FsmMgr { get; private set; }
        public static IResourceMgr ResourceMgr { get; private set; }
        public static ITimerMgr TimerMgr { get; private set; }
        public static INetworkMgr NetMgr { get; private set; }
        public static IObjectPoolMgr ObjectPoolMgr { get; private set; }
        public static IUIManager UIMgr { get; private set; }
        public static IConfigMgr CfgMgr { get; private set; }
        public static ILocalizationMgr LocalizationMgr
        { get; private set; }

        
        public void Initialize()
        {
            base.Awake();
            EventMgr = FrameworkMgr.GetModule<IEventMgr>();
            FsmMgr = FrameworkMgr.GetModule<IFsmMgr>();
            ResourceMgr = FrameworkMgr.GetModule<IResourceMgr>();
            TimerMgr = FrameworkMgr.GetModule<ITimerMgr>();
            NetMgr = FrameworkMgr.GetModule<INetworkMgr>();
            ObjectPoolMgr = FrameworkMgr.GetModule<IObjectPoolMgr>();
            UIMgr = FrameworkMgr.GetModule<IUIManager>();
            CfgMgr = FrameworkMgr.GetModule<IConfigMgr>();
            LocalizationMgr = FrameworkMgr.GetModule<ILocalizationMgr>();
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
