using GameEngine.Runtime.Procedure;
using ProcedureOwner = GameEngine.Runtime.Fsm.IFsm<GameEngine.Runtime.Procedure.IProcedureManager>;
namespace GameLauncher.Runtime.Procedure
{
    public class ProcedureStart : ProcedureBase
    {
        protected override void OnInit(ProcedureOwner procedureOwner)
        {
            base.OnInit(procedureOwner);
        }

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            // 语言配置：设置当前使用的语言，如果不设置，则默认使用操作系统语言。
            InitLanguageSettings();
            base.OnEnter(procedureOwner);
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
            ChangeState<ProcedureCheckVersion>(procedureOwner);
        }


        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            base.OnLeave(procedureOwner, isShutdown);
        }

        protected override void OnDestroy(ProcedureOwner procedureOwner)
        {
            base.OnDestroy(procedureOwner);
        }


        private void InitLanguageSettings()
        {
            
        }
    }
}

