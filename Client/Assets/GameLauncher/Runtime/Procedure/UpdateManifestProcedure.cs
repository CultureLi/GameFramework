using GameEngine.Runtime.Base.Procedure;
using YooAsset;
using Cysharp.Threading.Tasks;
using GameEngine.Runtime.Base;
using GameEngine.Runtime.Base.Variable;
using GameLauncher.Runtime.Event;

namespace GameLauncher.Runtime.Procedure
{
    internal class UpdateManifestProcedure : ProcedureBase
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
                arg.content = "更新资源清单...";
            });


            UpdateManifest().Forget();

        }

        private async UniTaskVoid UpdateManifest()
        {
            await UniTask.NextFrame();

            var PackageVersion = GlobalBlackboard.GetValue<VarString>("PackageVersion");

            bool savePackageVersion = true;
            var package = YooAssets.GetPackage("DefaultPackage");
            var operation = package.UpdatePackageManifestAsync(PackageVersion, savePackageVersion);
            await operation;

            if (operation.Status == EOperationStatus.Succeed)
            {
                ChangeState<CreateDownloaderProcedure>();
            }
            else
            {
                Log.Warning(operation.Error);
                LauncherEventMgr.Instance.BroadCast<UpdateManifestFailed>();
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
