using GameEngine.Runtime.Base.Setting;
using GameEngine.Runtime.Base;
using GameEngine.Runtime.Base.Launcher;
using GameEngine.Runtime.Base.Procedure;
using GameLauncher.Runtime.Procedure;
using System;
using UnityEngine;
using YooAsset;

namespace GameLauncher.Runtime
{
    public partial class Launcher : LauncherBase
    {
        [SerializeField]
        public LauncherSetting launcherSetting;

        private void Awake()
        {
            LauncherMgr.Instance.Init();
            YooAssets.Initialize();
            YooAssets.SetOperationSystemMaxTimeSlice(30);

            Initialize(new ProcedureBase[]{
                new StartProcedure(),
                new InitGlobalBlackboardProcedure(),
                new InitYooAssetProcedure(),
                new UpdateVersionProcedure(),
                new UpdateManifestProcedure(),
                new CreateDownloaderProcedure(),
                new DownloadingProcedure(),
                new DownloadFinishedProcedure(),
                new LoadDllProcedure(),
                new EndProcedure()
            });
            EntranceProcedure = typeof(StartProcedure);
        }

        private void Start()
        {
            base.Start();

            
        }

        private void Update()
        {
            base.Update();
        }

        private void OnDestroy()
        {
            base.OnDestroy();
        }

    }

}
