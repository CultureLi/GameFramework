using GameMain.Runtime.Module;
using GameMain.Runtime.Module.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameMain.Runtime
{
    internal class GameModule
    {
        public static EventModule EventModule = ModuleManager.Instance.GetModule<EventModule>();
    }
}
