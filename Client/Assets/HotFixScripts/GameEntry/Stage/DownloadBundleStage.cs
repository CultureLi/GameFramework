using Framework;
using UnityEngine;

namespace GameEntry.Stage
{
    internal class DownloadBundleStage : FsmState
    {
        MonoBehaviour _runner;
        public DownloadBundleStage(MonoBehaviour runner)
        {
            _runner = runner;
        }
        protected override void OnEnter()
        {
            FW.ResourceMgr.BundleDownloadCompleted += OnDownloadCompleted;
            _runner.StartCoroutine(FW.ResourceMgr.DownloadBundles());
        }

        protected override void OnLeave()
        {
            FW.ResourceMgr.BundleDownloadCompleted -= OnDownloadCompleted;
        }


        void OnDownloadCompleted()
        {
            ChangeState<EntranceEndStage>();
        }
    }
}
