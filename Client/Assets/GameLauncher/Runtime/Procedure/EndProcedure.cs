using GameEngine.Runtime.Base;
using GameEngine.Runtime.Base.Procedure;
using GameEngine.Runtime.Base.Utilitys;
using GameLauncher.Runtime.Event;
using System;
using System.Reflection;
using UnityEngine;

namespace GameLauncher.Runtime.Procedure
{
    public class EndProcedure : ProcedureBase
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
                arg.content = "启动结束，进入游戏...";
            });

            Assembly gameMain = Utility.Assembly.GetAssembly("GameEngine.Runtime.Logic");
            Log.Info($"Get Assembly{gameMain == null}");
            Type entry = gameMain.GetType("GameEngine.Runtime.Logic.GameEngineEntry");
            Log.Info($"Get entry {entry == null}");
            entry.GetMethod("Entry").Invoke(null,null);


            
            //GameObject.Destroy((Owner.Owner.Owner as Launcher).gameObject);
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

