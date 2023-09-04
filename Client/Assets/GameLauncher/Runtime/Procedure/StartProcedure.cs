using Assets.GameLauncher.Runtime.Procedure;
using dnlib.DotNet;
using GameEngine.Runtime.Base.Procedure;

namespace GameLauncher.Runtime.Procedure
{
    public class StartProcedure : ProcedureBase
    {
        protected override void OnInit()
        {
            base.OnInit();
        }

        protected override void OnEnter()
        {
            // 打开热更界面


            ChangeState<InitGlobalBlackboardProcedure>();
            base.OnEnter();
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate( elapseSeconds, realElapseSeconds);
            
        }


        protected override void OnLeave(bool isShutdown)
        {
            base.OnLeave( isShutdown);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

    }
}

