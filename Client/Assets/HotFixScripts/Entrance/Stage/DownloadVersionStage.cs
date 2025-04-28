using Entrance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Entrance.Stage
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
            _runner.StartCoroutine(EntranceMgr.I.DownloadWithRetry(
                PathDefine.remoteVersionUrl,
                null,
                3,
                10,
                OnDownloadCompleted));
        }

        void OnDownloadCompleted(DownloadHandler handler)
        {
            var localVersionAsset = Resources.Load<TextAsset>("version");
            var localVersion = localVersionAsset?.text ?? null;
            var remoteVersion = handler.text;

            if (!string.IsNullOrEmpty(localVersion) && localVersion.CompareTo(remoteVersion) < 0)
            {
                ChangeStage<DownloadCatalogHashStage>();
            }
            else
            {
                ChangeStage<ReloadCatalogStage>();
            }
        }
    }
}
