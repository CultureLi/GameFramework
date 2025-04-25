using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Launcher.Runtime.Stage
{
    internal class LauncherStartStage : StageBase
    {
        protected internal override void OnEnter()
        {
            ChangeStage<DownloadVersionStage>();
        }
    }
}
