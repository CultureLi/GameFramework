using Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GameEntry
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "ScriptableObjects/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        private static GameConfig _instance;
        private static readonly object locker = new();

        public static GameConfig I
        {
            get
            {
                if (_instance == null)
                {
                    lock (locker)
                    {
                        if (_instance == null)
                        {
                            _instance = Resources.Load<GameConfig>("GameConfig");
                        }
                    }
                }
                return _instance;
            }
        }

        [Tooltip("是否检查热更")]
        public bool checkHotUpdate;
    }
}
