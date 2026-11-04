using Assets.HotFixScripts.GameMain;
using Cysharp.Threading.Tasks;
using Framework;
using GameEntry;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameMain
{
    public class GameMainEntryMgr : Singleton<GameMainEntryMgr>
    {
        public static async UniTaskVoid Entry()
        {
            Utility.Test();
            Debug.Log("GameMainEntry");

            // 寻路 Demo 使用 PathFinding 测试场景（场景里的烘焙导航数据与服务端 navmesh.rnav 一致）
            UnityEngine.AsyncOperation sceneOperation;
            try
            {
                sceneOperation = SceneManager.LoadSceneAsync("PathFinding", LoadSceneMode.Single);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("PathFinding 场景没有加入 Build Settings：" + ex.Message);
                return;
            }

            if (sceneOperation == null)
            {
                Debug.LogError("PathFinding 场景没有加入 Build Settings，无法加载寻路 Demo");
                return;
            }

            while (!sceneOperation.isDone)
            {
                await UniTask.Yield();
            }

            await UniTask.WaitForEndOfFrame(FW.CoroutineRunner);
            new GameObject("GameMainEntryStages").AddComponent<GameMainEntryStages>();
        }
    }
}
