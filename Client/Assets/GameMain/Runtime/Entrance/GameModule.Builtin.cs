using GameEngine.Runtime.Module;
using GameEngine.Runtime.Module.Event;
using GameEngine.Runtime.Module.Timer;

namespace GameMain.Runtime.Entrance
{
    public static partial class GameModule
    {

        public static EventModule  EventModule
        { get; private set; }

        public static TimerModule TimerModule
        { get; private set; }


        public static void InitBuiltinModules()
        {
            EventModule = ModuleManager.Instance.GetModule<EventModule>();
            TimerModule = ModuleManager.Instance.GetModule<TimerModule>();
        }
    }
}
