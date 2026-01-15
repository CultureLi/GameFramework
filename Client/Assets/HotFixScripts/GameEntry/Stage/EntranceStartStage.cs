using Framework;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GameEntry.Stage
{
    internal class EntranceStartStage : FsmState
    {
        MonoBehaviour _runner;
        public EntranceStartStage(MonoBehaviour runner)
        {
            _runner = runner;
        }

        protected override void OnEnter()
        {
            FW.UIMgr.OpenWnd("GameEntry/UIGameEntryProgress");
            FW.UIMgr.OpenTips("Common/Tips/UICommonTips");
            ChangeState<DownloadHotfixDllStage>();
        }
    }
}
