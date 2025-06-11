using Framework;
using GameEntry.Stage;
using System.Collections.Generic;
using UnityEngine;

namespace GameEntry
{
    internal class GameEntryStages : MonoBehaviour
    {
        Fsm _stageFsm;
        void Awake()
        {
            _stageFsm = Fsm.Create("GameEntryFsm", new List<FsmState>()
            {
                new EntranceStartStage(),
                new DownloadHotfixDllStage(this),
                new DownloadCatalogStage(this),
                new DownloadBundleStage(this),
                new EntranceEndStage(),
            });
        }

        private void Start()
        {
            //如果在Awake中遇到Reentering the Update method，就挪到Start中，理论上是下一帧执行就可以了

            var uiRoot = new GameObject("UIRoot");
            GameEntry.UIMgr.AddUIGroup(0, uiRoot.transform);
            GameEntry.UIMgr.OpenUI("GameEntry/UIGameEntryProgress", 0);

            _stageFsm.Start<EntranceStartStage>();
        }
    }
}
