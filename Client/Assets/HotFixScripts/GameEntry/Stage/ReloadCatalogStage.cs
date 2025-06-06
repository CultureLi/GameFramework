/*using AOTBase;
using Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GameEntry.Stage
{
    internal class ReloadCatalogStage : FsmState
    {
        MonoBehaviour _runner;
        public ReloadCatalogStage(MonoBehaviour runner)
        {
            _runner = runner;
        }
        protected override void OnEnter()
        {
            _runner.StartCoroutine(FW.ResourceMgr.ReloadRemoteCatalog(PathDefine.remoteCatalogUrl, () =>
            {
                if (GameEntryMgr.I.IsCatalogHashChanged())
                    ChangeState<DownloadBundleStage>();
                else
                    ChangeState<EntranceEndStage>();
            }));
        }
    }
}
*/