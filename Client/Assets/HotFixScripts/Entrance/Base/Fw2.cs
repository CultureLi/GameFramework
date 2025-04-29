using Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entrance
{
    internal class Fw2 : SingletonMono<Fw2>
    {
        public static IResourceMgr resourceMgr;

        private void Awake()
        {
            InitModules();
        }

        void InitModules()
        {
            resourceMgr = FrameworkEntry.GetModule<IResourceMgr>();
        }
    }
}
