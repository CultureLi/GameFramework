using Framework;
using GameEntry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameMain
{
    internal class MainEntryStartStage : FsmState
    {
        protected override void OnEnter()
        {
            FW.UIMgr.OpenHud("Main/UIMain");

            FW.GameLogicMgr.CreateMgr<PlayerInfoMgr>();

            PlayerInfoMgr.I.Test();

            // 寻路 Demo 只在 PathFinding 场景里创建
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "PathFinding")
            {
                new UnityEngine.GameObject("PathFindingDemo").AddComponent<PathFindingDemo>();
            }
        }
    }
}
