using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entrance.Stage
{
    internal class EntranceStartStage : StageBase
    {
        protected internal override void OnEnter()
        {
            ChangeStage<DownloadVersionStage>();
        }
    }
}
