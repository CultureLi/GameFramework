using GameEngine.Runtime.Base;
using UnityEngine;

namespace GameMain.Runtime.Entrance
{
    public partial class GameEntry:MonoBehaviour
    {
        public static void Entry()
        {
            Log.Debug("GameMainEntry");
            var go = new GameObject("GameEntry");            
            go.AddComponent<GameEntry>();
        }
    }
}
