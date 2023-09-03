using GameEngine.Runtime.Base.Procedure;
using GameEngine.Runtime.Base.Utilitys;
using System;
using System.Reflection;
using UnityEngine;

namespace GameLauncher.Runtime.Procedure
{
    public class ProcedureFinished : ProcedureBase
    {
        protected override void OnInit()
        {
            base.OnInit();
        }

        protected override void OnEnter()
        {
            base.OnEnter();
            Assembly gameMain = Utility.Assembly.GetAssembly("GameEngine.Runtime.Logic");
            Type entry = gameMain.GetType("GameEngine.Runtime.Logic.GameEngineEntry");
            entry.GetMethod("Entry").Invoke(null,null);

            
            GameObject.Destroy((Owner.Owner.Owner as Launcher).gameObject);
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

