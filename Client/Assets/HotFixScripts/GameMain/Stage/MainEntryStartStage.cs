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

            // Main 场景的正式寻路管理器：玩家、寻路消息、关卡门同步
            var pathfinding = FW.GameLogicMgr.CreateMgr<PathfindingMgr>();
            pathfinding.Initialize();

            // 地图编辑器导出的 Building/Decorate/Resource 数据，按玩家位置流式创建
            var mapData = FW.GameLogicMgr.CreateMgr<MapDataMgr>();
            mapData.Initialize();
        }
    }
}
