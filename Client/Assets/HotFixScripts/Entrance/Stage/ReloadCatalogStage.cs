using Entrance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entrance.Stage
{
    internal class ReloadCatalogStage : StageBase
    {
        protected internal override void OnEnter()
        {
            EntranceMgr.I.ReloadCatalog();

            if (EntranceMgr.I.IsCatalogHashChanged())
                ChangeStage<DownloadBundleStage>();
            else
                ChangeStage<EntranceEndStage>();

        }
    }
}
