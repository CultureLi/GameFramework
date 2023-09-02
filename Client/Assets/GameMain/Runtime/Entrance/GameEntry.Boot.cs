using GameEngine.Runtime.Base;
using GameEngine.Runtime.Module;
using UnityEngine;

namespace GameMain.Runtime.Entrance
{
    public class GameEntry
    {
        public static void Entry()
        {
            Log.Debug("GameMainEntry");

            ModuleManager.Instance.GetModule<GameMainModule>();
        }

        
    }
}
