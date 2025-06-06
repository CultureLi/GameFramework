using Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameEntry.Stage
{
    public class EntranceStartStage : FsmState
    {
        protected override void OnEnter()
        {
            GameEntryMgr.I.LoadingSceneHandle = FW.ResourceMgr.LoadSceneAsync("Assets/BundleRes/Scene/Login.unity");
            GameEntryMgr.I.LoadingSceneHandle.WaitForCompletion();

            ChangeState<DownloadHotfixDllStage>();
        }
    }
}
