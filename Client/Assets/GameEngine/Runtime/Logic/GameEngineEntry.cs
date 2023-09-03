using GameEngine.Runtime.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GameEngine.Runtime.Logic
{
    public class GameEngineEntry
    {
        public static void Entry()
        {
            Log.Debug("GameEngineEntry");
            new GameObject("GameEngineLauncher").AddComponent<GameEngineLauncher>();
        }
    }
}
