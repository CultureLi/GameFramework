using GameLauncher.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.GameLauncher.Runtime.Stage
{
    internal class ReloadCatalogStage : StageBase
    {
        protected internal override void OnEnter()
        {
            LauncherMgr.I.ReloadCatalog();

            if (LauncherMgr.I.isCatalogHashChanged)
                Owner.ChangeStage<DownloadBundleStage>();
            else
                Owner.ChangeStage<LauncherEndStage>();
        }
    }
}
