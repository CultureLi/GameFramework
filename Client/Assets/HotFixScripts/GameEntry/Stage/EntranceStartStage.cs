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
            ChangeState<DownloadVersionStage>();
        }
    }
}
