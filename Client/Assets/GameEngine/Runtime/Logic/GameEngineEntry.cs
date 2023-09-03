using GameEngine.Runtime.Base;
using UnityEngine;

namespace GameEngine.Runtime.Logic
{
    public class GameEngineEntry
    {
        public static void Entry()
        {
            Log.Debug("GameEngineEntry");
            var go = new GameObject("GameEngineLauncher");
            go.AddComponent<ConsoleToScreen>();
            go.AddComponent<GameEngineLauncher>();
        }
    }
}
