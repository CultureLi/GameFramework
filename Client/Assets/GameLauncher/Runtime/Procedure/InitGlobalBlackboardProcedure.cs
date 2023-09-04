using Assets.GameEngine.Runtime.Base.Setting;
using GameEngine.Runtime.Base;
using GameEngine.Runtime.Base.Procedure;
using GameLauncher.Runtime.Procedure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.GameLauncher.Runtime.Procedure
{
    /// <summary>
    /// 设置全局黑板参数
    /// </summary>
    internal class InitGlobalBlackboardProcedure:ProcedureBase
    {
        protected override void OnInit()
        {
            base.OnInit();
        }

        protected override void OnEnter()
        {
            base.OnEnter();

            GlobalBlackboard.SetValue("LauncherSetting", new LauncherSetting());
            ChangeState<ProcedureCheckVersion>();

        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);
        }


        protected override void OnLeave(bool isShutdown)
        {
            base.OnLeave(isShutdown);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
