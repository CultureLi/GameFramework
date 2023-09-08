using GameEngine.Runtime.Base.Procedure;
using UnityEngine;
using YooAsset;
using Cysharp.Threading.Tasks;
using GameEngine.Runtime.Base;
using GameLauncher.Runtime.Event;

namespace GameLauncher.Runtime.Procedure
{
    internal class CreateDownloaderProcedure : ProcedureBase
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
                arg.content = "创建下载器...";
            });

            CreateDownloader().Forget();

        }

        private async UniTaskVoid CreateDownloader()
        {
            await UniTask.NextFrame();

            int downloadingMaxNum = 10;
            int failedTryAgain = 3;
            var downloader = YooAssets.CreateResourceDownloader(downloadingMaxNum, failedTryAgain);
            LauncherMgr.Instance.Downloader = downloader;

            if (downloader.TotalDownloadCount == 0)
            {
                Log.Info("Not found any download files !");
                ChangeState<DownloadFinishedProcedure>();
            }
            else
            {
                //A total of 10 files were found that need to be downloaded
                Log.Info($"Found total {downloader.TotalDownloadCount} files that need download ！");

                // 发现新更新文件后，挂起流程系统
                // 注意：开发者需要在下载前检测磁盘空间不足
                int totalDownloadCount = downloader.TotalDownloadCount;
                long totalDownloadBytes = downloader.TotalDownloadBytes;
                LauncherEventMgr.Instance.BroadCast<UpdateFilesInfo>(arg =>
                {
                    arg.num = totalDownloadCount;
                    arg.size = totalDownloadBytes;
                });
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
