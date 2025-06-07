using Framework;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameEntry.Stage
{
    public class EntranceStartStage : FsmState
    {
        protected override void OnEnter()
        {
            Debug.Log("加载Login场景");
            GameEntryMgr.I.LoadingSceneHandle = FW.ResourceMgr.LoadSceneAsync("Assets/BundleRes/Scene/Login.unity");
            GameEntryMgr.I.LoadingSceneHandle.AddCompleted(handle =>
            {
                Debug.Log($"Status: {GameEntryMgr.I.LoadingSceneHandle.Status}");
                SceneManager.SetActiveScene(handle.Result.Scene);
                ChangeState<DownloadHotfixDllStage>();
            });
        }
    }
}
