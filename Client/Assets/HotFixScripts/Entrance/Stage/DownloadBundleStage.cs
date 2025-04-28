using Entrance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Entrance.Stage
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
            EntranceMgr.I.BundleDownloadCompleted += OnDownloadCompleted;
            _runner.StartCoroutine(EntranceMgr.I.DownloadBundles());
        }

        protected internal override void OnExit()
        {
            EntranceMgr.I.BundleDownloadCompleted -= OnDownloadCompleted;
        }


        void OnDownloadCompleted()
        {
            Owner.ChangeStage<EntranceEndStage>();
        }
    }
}
