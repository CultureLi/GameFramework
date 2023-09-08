using GameEngine.Runtime.Base.Procedure;
using YooAsset;
using Cysharp.Threading.Tasks;
using System.Threading;
using GameLauncher.Runtime.Event;

namespace GameLauncher.Runtime.Procedure
{
    internal class DownloadingProcedure : ProcedureBase
    {
        protected override void OnInit()
        {
            base.OnInit();
        }

        protected override void OnEnter()
        {
            base.OnEnter();
            LauncherEventMgr.Instance.BroadCast<CommonMessageEvent>(arg =>
            {
                arg.content = "开始下载文件...";
            });

            BeginDownload().Forget();

        }

        private async UniTaskVoid BeginDownload()
        {
            var downloader = LauncherMgr.Instance.Downloader;


            // 注册下载回调
            downloader.OnDownloadErrorCallback = LauncherMgr.Instance.OnDownloadErrorCallback;
            downloader.OnDownloadProgressCallback = LauncherMgr.Instance.OnDownloadProgressCallback;
            downloader.BeginDownload();
            await downloader;

            // 检测下载结果
            if (downloader.Status == EOperationStatus.Succeed)
            {
                ChangeState<DownloadFinishedProcedure>();
            }
            else
            {
                LauncherEventMgr.Instance.BroadCast<DownloadingFileFailed>();
            }

        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

        }


        protected override void OnLeave(bool isShutdown)
        {
            base.OnLeave(isShutdown);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
