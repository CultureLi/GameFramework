using GameEngine.Runtime.Module;
using GameMain.Runtime.Events;
using Bright.Serialization;
using GameEngine.Runtime.Module;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using GameEngine.Runtime.Base;

namespace GameMain.Runtime.CustomModule
{
    public partial class GameMainModule:ModuleBase
    {
        public override void OnInit(InitModuleParamBase param)
        {
            Log.Info("GameMainModule Init ... ");
        }

        public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            Log.Info("GameMainModule Update ... ");
        }

        public override void OnFixUpdate(float elapseSeconds, float realElapseSeconds)
        {

        }


        public override void OnLateUpdate(float elapseSeconds, float realElapseSeconds)
        {
        }


        public override void Release()
        {
        }

       
    }
}
