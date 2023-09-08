using GameEngine.Runtime.Base.Procedure;
using YooAsset;
using Cysharp.Threading.Tasks;
using GameEngine.Runtime.Base;
using GameEngine.Runtime.Base.Variable;
using GameLauncher.Runtime.Event;

namespace GameLauncher.Runtime.Procedure
{
    internal class UpdateVersionProcedure:ProcedureBase
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
                arg.content = "更新资源版本";
            });


            GetStaticVersion().Forget();

        }

        private async UniTaskVoid GetStaticVersion()
        {
            await UniTask.NextFrame();

            var package = YooAssets.GetPackage("DefaultPackage");
            var operation = package.UpdatePackageVersionAsync();
            await operation;

            if (operation.Status == EOperationStatus.Succeed)
            {
                var versionVar = new VarString();
                versionVar = operation.PackageVersion;
                GlobalBlackboard.SetValue("PackageVersion", versionVar);
                Log.Info($"远端最新版本为: {operation.PackageVersion}");
                ChangeState<UpdateManifestProcedure>();
            }
            else
            {
                Log.Warning(operation.Error);
                LauncherEventMgr.Instance.BroadCast<UpdateVersionFailed>();
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
