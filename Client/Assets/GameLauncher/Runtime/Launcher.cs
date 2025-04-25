using Assets.GameLauncher.Runtime.Stage;
using Launcher.Runtime.Stage;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Launcher.Runtime
{
    public partial class Launcher : MonoBehaviour
    {
        StageMgr stageMgr = new StageMgr();
        private void Awake()
        {
            Addressables.InitializeAsync().WaitForCompletion();

            stageMgr.AddStage(new LauncherStartStage());
            stageMgr.AddStage(new DownloadVersionStage(this));
            stageMgr.AddStage(new DownloadCatalogHashStage(this));
            stageMgr.AddStage(new DownloadCatalogStage(this));
            stageMgr.AddStage(new DownloadBundleStage(this));
            stageMgr.AddStage(new ReloadCatalogStage());
            stageMgr.AddStage(new OverrideCatalogHashStage());
            stageMgr.AddStage(new LoadDllStage());
            stageMgr.AddStage(new LauncherEndStage());
            stageMgr.AddStage(new DownloadHotfixDll(this));

            stageMgr.ChangeStage<DownloadHotfixDll>();
        }

        private void Start()
        {

        }

        private void Update()
        {
            stageMgr.Update(Time.deltaTime, Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {

        }
    }

}
