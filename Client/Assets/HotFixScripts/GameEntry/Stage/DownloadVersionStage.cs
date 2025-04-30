
using Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace GameEntry.Stage
{
    /// <summary>
    /// 下载版本信息，检查是否需要热更，此版本为AppVersion
    /// </summary>
    internal class DownloadVersionStage : FsmState
    {
        MonoBehaviour _runner;
        public DownloadVersionStage(MonoBehaviour runner)
        {
            _runner = runner;
        }

        protected override void OnEnter()
        {
            _runner.StartCoroutine(FW.ResourceMgr.LoadFile(
                PathDefine.remoteVersionUrl,
                OnDownloadCompleted));
        }

        void OnDownloadCompleted(DownloadHandler handler)
        {
            var localVersionAsset = Resources.Load<TextAsset>("version");
            var localVersion = localVersionAsset?.text ?? null;
            var remoteVersion = handler.text;

            if (!string.IsNullOrEmpty(localVersion) && localVersion.CompareTo(remoteVersion) < 0)
            {
                ChangeState<DownloadCatalogHashStage>();
            }
            else
            {
                ChangeState<ReloadCatalogStage>();
            }
        }
    }
}
