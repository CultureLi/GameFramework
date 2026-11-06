using Assets.HotFixScripts.GameMain;
using Cysharp.Threading.Tasks;
using Framework;
using GameEntry;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace GameMain
{
    public class GameMainEntryMgr : Singleton<GameMainEntryMgr>
    {
        public static async UniTaskVoid Entry()
        {
            Utility.Test();
            Debug.Log("GameMainEntry");

            // Main 是 Addressable 场景
            AsyncOperationHandle<SceneInstance> sceneOperation;
            try
            {
                sceneOperation = FW.ResMgr.LoadSceneAsync("Main");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Main 场景加载失败：" + ex.Message);
                return;
            }

            if (sceneOperation.Status == AsyncOperationStatus.Failed)
            {
                Debug.LogError("Main 场景加载失败");
                return;
            }

            while (!sceneOperation.IsDone)
            {
                await UniTask.Yield();
            }

            await UniTask.WaitForEndOfFrame(FW.CoroutineRunner);
            new GameObject("GameMainEntryStages").AddComponent<GameMainEntryStages>();
        }
    }
}
