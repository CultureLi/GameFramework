using GameEngine.Runtime.Base;
using GameEngine.Runtime.Base.Procedure;
using GameEngine.Runtime.Fsm;
using GameLauncher.Runtime.Procedure;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameLauncher.Runtime
{
    public partial class Launcher : LauncherBase
    {
        [SerializeField]
        private AssetLoadMode assetLoadMode;

        private void Awake()
        {
            Initialize(new ProcedureBase[]{
                new ProcedureStart(),
                new ProcedureCheckVersion(),
                new ProcedureHotUpdate(),
                new ProcedureLoadDll(),
                new ProcedureFinished()
            });
            EntranceProcedure = typeof(ProcedureStart);
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
