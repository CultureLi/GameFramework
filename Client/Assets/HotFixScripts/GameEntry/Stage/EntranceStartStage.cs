using Framework;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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

            FW.UIMgr.OpenUI("GameEntry/UIGameEntryProgress", (int)UIGroupType.Normal);

            ChangeState<DownloadHotfixDllStage>();
        }
    }
}
