using GameEngine.Runtime.Base.Procedure;

namespace GameLauncher.Runtime.Procedure
{
    public class ProcedureStart : ProcedureBase
    {
        protected override void OnInit()
        {
            base.OnInit();
        }

        protected override void OnEnter()
        {
            // 语言配置：设置当前使用的语言，如果不设置，则默认使用操作系统语言。
        
            base.OnEnter();
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate( elapseSeconds, realElapseSeconds);
            ChangeState<ProcedureCheckVersion>();
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

