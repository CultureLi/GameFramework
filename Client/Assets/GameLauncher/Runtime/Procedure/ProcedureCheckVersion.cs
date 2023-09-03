using GameEngine.Runtime.Base.Procedure;

namespace GameLauncher.Runtime.Procedure
{
    public class ProcedureCheckVersion : ProcedureBase
    {
        private bool m_HasNewVersion = false;
        protected override void OnInit()
        {
            base.OnInit();
        }

        protected override void OnEnter()
        {
            base.OnEnter();
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (m_HasNewVersion)
            {
                ChangeState<ProcedureHotUpdate>();
            }
            else
            {
                ChangeState<ProcedureLoadDll>();
            }
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

