using GameMain.Runtime.Base;
using GameMain.Runtime.Base.Utilitys;
using UnityEngine;

namespace GameMain.Runtime.Logic
{
    public class GameMainEntry
    {
        public static void Entry()
        {
            Debug.Log(" 滴滴滴 GameMainEntry");
            Utility.Test();
        }
    }
}
