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
    /// <summary>
    /// 下载版本信息，检查是否需要热更，此版本为AppVersion
    /// </summary>
    internal class DownloadVersionStage : StageBase
    {
        MonoBehaviour _runner;
        public DownloadVersionStage(MonoBehaviour runner)
        {
            _runner = runner;
        }

        protected internal override void OnEnter()
        {
            _runner.StartCoroutine(LauncherMgr.I.DownloadWithRetry(
                PathDefine.remoteVersionUrl,
                null,
                3,
                10,
                OnDownloadCompleted));
        }

        void OnDownloadCompleted(DownloadHandler handler)
        {
            var localVersion = Resources.Load<TextAsset>("version.txt").text;
            var remoteVersion = handler.text;

            if (string.Equals(localVersion, remoteVersion))
            {
                Owner.ChangeStage<DownloadCatalogHashStage>();
            }
            else
            {
                LauncherMgr.I.ForceUpdateApp?.Invoke();
            }
        }
    }
}
