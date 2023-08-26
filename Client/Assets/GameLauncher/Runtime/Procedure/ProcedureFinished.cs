using GameEngine.Runtime.Procedure;
using GameEngine.Runtime.Utilitys;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using ProcedureOwner = GameEngine.Runtime.Fsm.IFsm<GameEngine.Runtime.Procedure.IProcedureManager>;
namespace GameLauncher.Runtime.Procedure
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
            //var entry = Utility.Assembly.GetType("GameMain.Runtime.Entrance.GameEntry");
            //if (entry == null)
            //    throw new Exception("GameEntry Not Found!!!");
#if !UNITY_EDITOR
            Assembly.Load(File.ReadAllBytes($"{Application.streamingAssetsPath}/GameEngine.Runtime.dll.bytes"));

            Assembly gameMain = Assembly.Load(File.ReadAllBytes($"{Application.streamingAssetsPath}/GameMain.Runtime.dll.bytes"));
#else
            Assembly gameMain = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "GameMain.Runtime");

#endif
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

