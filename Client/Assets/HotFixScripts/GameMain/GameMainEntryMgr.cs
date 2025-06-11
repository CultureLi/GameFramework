using Assets.HotFixScripts.GameMain;
using Cysharp.Threading.Tasks;
using Framework;
using GameEntry;
using UnityEngine;

namespace GameMain
{
    public class GameMainEntryMgr : Singleton<GameMainEntryMgr>
    {
        public static async UniTaskVoid Entry()
        {
            Utility.Test();
            Debug.Log(" 慢慢慢 GameMainEntry");

            var handle = FW.ResourceMgr.LoadSceneAsync("Main");
            await handle.ToUniTask();

            FW.UIMgr.CloseAllUI();

            await UniTask.NextFrame();
            new GameObject("GameMainEntryStages").AddComponent<GameMainEntryStages>();
        }
    }
}
