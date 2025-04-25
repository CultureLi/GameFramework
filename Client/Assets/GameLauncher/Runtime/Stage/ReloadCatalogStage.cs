using Launcher.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Launcher.Runtime.Stage
{
    internal class ReloadCatalogStage : StageBase
    {
        protected internal override void OnEnter()
        {
            LauncherMgr.I.ReloadCatalog();

            if (LauncherMgr.I.isCatalogHashChanged)
                Owner.ChangeStage<DownloadBundleStage>();
            else
                Owner.ChangeStage<LoadDllStage>();
        }
    }
}
