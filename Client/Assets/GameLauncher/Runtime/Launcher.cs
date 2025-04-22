using Assets.GameLauncher.Runtime.Stage;
using System;
using UnityEngine;

namespace GameLauncher.Runtime
{
    public partial class Launcher : MonoBehaviour
    {
        StageMgr stageMgr = new StageMgr();
        private void Awake()
        {
            stageMgr.AddStage(new LauncherStartStage());
            stageMgr.AddStage(new DownloadVersionStage(this));
            stageMgr.AddStage(new DownloadCatalogHashStage(this));
            stageMgr.AddStage(new DownloadCatalogStage(this));
            stageMgr.AddStage(new DownloadBundleStage(this));
            stageMgr.AddStage(new ReloadCatalogStage());
            stageMgr.AddStage(new OverrideCatalogHashStage());
            stageMgr.AddStage(new LauncherEndStage());

            stageMgr.ChangeStage<LauncherStartStage>();
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
