using Framework;
using UnityEngine;

namespace GameEntry
{
    internal class FW : MonoBehaviour
    {
        public static IEventMgr EventMgr { get; private set; }
        public static IFsmMgr FsmMgr { get; private set; }
        public static IResourceMgr ResourceMgr { get; private set; }

        void Awake()
        {
            InitModules();
        }

        void InitModules()
        {
            EventMgr = FrameworkMgr.GetModule<IEventMgr>();
            FsmMgr = FrameworkMgr.GetModule<IFsmMgr>();
            ResourceMgr = FrameworkMgr.GetModule<IResourceMgr>();
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
