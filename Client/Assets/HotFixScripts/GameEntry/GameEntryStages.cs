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
                new DownloadVersionStage(this),
                new DownloadCatalogHashStage(this),
                new ReloadCatalogStage(this),
                new DownloadBundleStage(this),
                new EntranceEndStage(),
            });

            _stageFsm.Start<EntranceStartStage>();
        }
    }
}
