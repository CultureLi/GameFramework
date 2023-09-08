using GameLauncher.Runtime.Event;
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
            base.OnEnter();

            LauncherEventMgr.Instance.BroadCast<CommonMessageEvent>(arg =>
            {
                arg.content = "开启启动流程";
            });

            // 打开热更界面


            ChangeState<InitGlobalBlackboardProcedure>();
         
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

