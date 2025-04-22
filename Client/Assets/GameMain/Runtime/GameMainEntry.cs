using GameMain.Runtime.Base;
using GameMain.Runtime.Module;
using GameMain.Runtime.CustomModule;

namespace GameMain.Runtime
{
    public class GameMainEntry
    {
        public static void Entry()
        {
            Log.Debug("GameMainEntry");

            ModuleManager.Instance.GetModule<GameMainModule>();
        }

        
    }
}
