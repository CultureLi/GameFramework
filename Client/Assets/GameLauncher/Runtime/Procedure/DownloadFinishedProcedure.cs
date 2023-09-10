using GameEngine.Runtime.Base.Procedure;
using GameLauncher.Runtime.Event;

namespace GameLauncher.Runtime.Procedure
{
    internal class DownloadFinishedProcedure : ProcedureBase
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
                arg.content = "下载完毕，清理缓存...";
            });

            ChangeState<LoadDllProcedure>();
            //var package = YooAsset.YooAssets.GetPackage("DefaultPackage");
            //var operation = package.ClearUnusedCacheFilesAsync();
            //operation.Completed += OperationCompleted;
        }

        private void OperationCompleted(YooAsset.AsyncOperationBase obj)
        {
            ChangeState<LoadDllProcedure>();
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
