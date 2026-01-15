using Framework;
using GameEntry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameMain
{
    internal class MainEntryStartStage : FsmState
    {
        protected override void OnEnter()
        {
            FW.UIMgr.OpenHud("Main/UIMain");
        }
    }
}
