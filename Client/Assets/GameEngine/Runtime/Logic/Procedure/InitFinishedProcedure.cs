using GameEngine.Runtime.Base.Procedure;
using GameEngine.Runtime.Base.Utilitys;
using System;
using System.Reflection;

namespace GameEngine.Runtime.Logic.Procedure
{
    internal class InitFinishedProcedure : ProcedureBase
    {
        protected override void OnInit()
        {
            base.OnInit();
        }

        protected override void OnEnter()
        {
            base.OnEnter();
            
            Assembly gameMain = Utility.Assembly.GetAssembly("GameMain.Runtime");
            Type entry = gameMain.GetType("GameMain.Runtime.GameMainEntry");
            entry.GetMethod("Entry").Invoke(null, null);

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
