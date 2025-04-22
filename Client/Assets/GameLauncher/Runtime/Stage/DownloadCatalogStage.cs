using GameLauncher.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.GameLauncher.Runtime.Stage
{
    internal class DownloadCatalogStage : StageBase
    {
        MonoBehaviour _runner;
        public DownloadCatalogStage(MonoBehaviour runner)
        {
            _runner = runner;
        }
        protected internal override void OnEnter()
        {
            _runner.StartCoroutine(LauncherMgr.I.DownloadWithRetry(
                PathDefine.remoteCatalogUrl,
                PathDefine.newestCalalogPath,
                3,
                10,
                OnDownloadCompleted));
        }
        void OnDownloadCompleted(DownloadHandler handler)
        {
            Owner.ChangeStage<ReloadCatalogStage>();
        }
    }
}
