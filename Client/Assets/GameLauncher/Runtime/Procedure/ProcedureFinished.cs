using GameEngine.Runtime.Base.Procedure;
using GameEngine.Runtime.Base.Utilitys;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using ProcedureOwner = GameEngine.Runtime.Fsm.IFsm<GameEngine.Runtime.Base.Procedure.IProcedureManager>;
namespace GameLauncher.Runtime.Base.Procedure
{
    public class ProcedureFinished : ProcedureBase
    {
        protected override void OnInit(ProcedureOwner procedureOwner)
        {
            base.OnInit(procedureOwner);
        }

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);
            Assembly gameMain = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "GameMain.Runtime");
            Type entry = gameMain.GetType("GameMain.Runtime.Entrance.GameEntry");
            entry.GetMethod("Entry").Invoke(null,null);
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
            
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

