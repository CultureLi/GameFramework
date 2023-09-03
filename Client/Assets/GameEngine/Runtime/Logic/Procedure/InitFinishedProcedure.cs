using GameEngine.Runtime.Base.Procedure;
using GameEngine.Runtime.Base.Utilitys;
using GameEngine.Runtime.Fsm;
using System;
using System.Linq;
using System.Reflection;

namespace GameEngine.Runtime.Logic.Procedure
{
    internal class InitFinishedProcedure : ProcedureBase
    {
        protected override void OnInit(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnInit(procedureOwner);
        }

        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            
            Assembly gameMain = Utility.Assembly.GetAssembly("GameMain.Runtime");
            Type entry = gameMain.GetType("GameMain.Runtime.GameMainEntry");
            entry.GetMethod("Entry").Invoke(null, null);

        }

        protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

        }


        protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
        {
            base.OnLeave(procedureOwner, isShutdown);
        }

        protected override void OnDestroy(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnDestroy(procedureOwner);
        }

    }
}
