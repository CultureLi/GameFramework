using Framework;
using GameEntry.Stage;
using System.Collections.Generic;
using UnityEngine;

namespace GameEntry
{
    internal class EntranceStages : MonoBehaviour
    {
        Fsm stageFsm;
        void Awake()
        {
            stageFsm = Fsm.Create("GameEntryFsm", new List<FsmState>()
            {
                new EntranceStartStage(),
                new DownloadVersionStage(this),
                new DownloadCatalogHashStage(this),
                new ReloadCatalogStage(this),
                new DownloadBundleStage(this),
                new EntranceEndStage(),
            });

            stageFsm.Start<EntranceStartStage>();
        }
    }
}
