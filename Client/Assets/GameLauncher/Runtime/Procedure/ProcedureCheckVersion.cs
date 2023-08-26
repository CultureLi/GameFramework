using GameEngine.Runtime.Procedure;
using ProcedureOwner = GameEngine.Runtime.Fsm.IFsm<GameEngine.Runtime.Procedure.IProcedureManager>;

namespace GameLauncher.Runtime.Procedure
{
    public class ProcedureCheckVersion : ProcedureBase
    {
        private bool m_HasNewVersion = false;
        protected override void OnInit(ProcedureOwner procedureOwner)
        {
            base.OnInit(procedureOwner);
        }

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {

            base.OnEnter(procedureOwner);
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            if (m_HasNewVersion)
            {
                ChangeState<ProcedureHotUpdate>(procedureOwner);
            }
            else
            {
                ChangeState<ProcedureFinished>(procedureOwner);
            }
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            base.OnLeave(procedureOwner, isShutdown);
        }

        protected override void OnDestroy(ProcedureOwner procedureOwner)
        {
            base.OnDestroy(procedureOwner);
        }

    }
}

