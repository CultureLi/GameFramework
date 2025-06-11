using Framework;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameEntry.Stage
{
    internal class EntranceStartStage : FsmState
    {
        protected override void OnEnter()
        {
            if (GameEntryMgr.I.UIRoot == null)
            {
                var handle = FW.ResourceMgr.InstantiateAsync("Assets/BundleRes/UI/Root/UIRoot.prefab");
                handle.WaitForCompletion();
                GameObject.DontDestroyOnLoad(handle.Result);
                GameEntryMgr.I.UIRoot = handle.Result.transform;
            }

            var uiRoot = new GameObject("UIRoottemp");
            FW.UIMgr.OpenUI("GameEntry/UIGameEntryProgress", (int)UIGroupType.Normal);

            ChangeState<DownloadHotfixDllStage>();
        }
    }
}
