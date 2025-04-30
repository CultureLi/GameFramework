using Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameEntry
{
    internal class FW : SingletonMono<FW>
    {
        public static IEventMgr EventMgr { get; private set; }
        public static IFsmMgr FsmMgr { get; private set; }
        public static IResourceMgr ResourceMgr { get; private set; }

        private void Awake()
        {
            InitModules();
        }

        void InitModules()
        {
            EventMgr = FrameworkEntry.GetModule<IEventMgr>();
            FsmMgr = FrameworkEntry.GetModule<IFsmMgr>();
            ResourceMgr = FrameworkEntry.GetModule<IResourceMgr>();
        }
    }
}
