using Framework;
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
            ChangeState<InitUIRootStage>();
        }
    }
}
