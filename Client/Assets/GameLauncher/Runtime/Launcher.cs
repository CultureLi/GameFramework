using Assets.GameEngine.Runtime.Base.Setting;
using GameEngine.Runtime.Base;
using GameEngine.Runtime.Base.Launcher;
using GameEngine.Runtime.Base.Procedure;
using GameLauncher.Runtime.Procedure;
using System;
using UnityEngine;

namespace GameLauncher.Runtime
{
    public partial class Launcher : LauncherBase
    {
        [SerializeField]
        public LauncherSetting launcherSetting;

        private void Awake()
        {
            Initialize(new ProcedureBase[]{
                new StartProcedure(),
                new ProcedureCheckVersion(),
                new ProcedureHotUpdate(),
                new ProcedureLoadDll(),
                new ProcedureFinished()
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
