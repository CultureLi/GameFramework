using Launcher.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Launcher.Runtime.Stage
{
    internal class OverrideCatalogHashStage : StageBase
    {
        protected internal override void OnEnter()
        {
            var jsonText = File.ReadAllText(PathDefine.tempCalalogHashPath);
            File.WriteAllText(PathDefine.newestCalalogHashPath, jsonText);

            Owner.ChangeStage<LoadDllStage>();
        }
    }
}
