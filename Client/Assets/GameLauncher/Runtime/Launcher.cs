using GameEngine.Runtime.Base;
using GameEngine.Runtime.Base.Procedure;
using GameEngine.Runtime.Fsm;
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
            base.Awake();
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
