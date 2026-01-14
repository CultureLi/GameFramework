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
            Debug.Log("GameMainEntry");

            await FW.ResMgr.LoadSceneAsync("Main").ToUniTask(FW.CoroutineRunner);

            await UniTask.WaitForEndOfFrame(FW.CoroutineRunner);
            new GameObject("GameMainEntryStages").AddComponent<GameMainEntryStages>();
        }
    }
}
