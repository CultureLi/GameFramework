using Entrance.Stage;
using System.Collections.Generic;
using UnityEngine;

namespace Entrance
{
    internal class EntranceStages : MonoBehaviour
    {
        StageMgr stageMgr;
        void Awake()
        {
            stageMgr = new StageMgr(new List<StageBase>()
            {
                new EntranceStartStage(),
                new DownloadVersionStage(this),
                new DownloadCatalogHashStage(this),
                new DownloadCatalogStage(this),
                new ReloadCatalogStage(),
                new DownloadBundleStage(this),
                new EntranceEndStage(),
            });

            stageMgr.ChangeStage<EntranceStartStage>();
        }
    }
}
