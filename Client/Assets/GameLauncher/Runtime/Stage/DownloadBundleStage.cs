using Launcher.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Launcher.Runtime.Stage
{
    internal class DownloadBundleStage : StageBase
    {
        MonoBehaviour _runner;
        public DownloadBundleStage(MonoBehaviour runner)
        {
            _runner = runner;
        }
        protected internal override void OnEnter()
        {
            LauncherMgr.I.BundleDownloadCompleted += OnDownloadCompleted;
            _runner.StartCoroutine(LauncherMgr.I.DownloadBundles());
        }

        protected internal override void OnExit()
        {
            LauncherMgr.I.BundleDownloadCompleted -= OnDownloadCompleted;
        }


        void OnDownloadCompleted()
        {
            Owner.ChangeStage<OverrideCatalogHashStage>();
        }
    }
}
