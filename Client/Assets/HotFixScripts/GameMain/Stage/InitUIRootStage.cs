using Cysharp.Threading.Tasks;
using Framework;
using GameEntry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GameMain
{
    internal class InitUIRootStage : FsmState
    {
        MonoBehaviour _runner;
        public InitUIRootStage(MonoBehaviour runner)
        {
            _runner = runner;
        }
        protected override async void OnEnter()
        {
            FW.UIMgr.OpenHud("Main/UIMain");
        }
    }
}
