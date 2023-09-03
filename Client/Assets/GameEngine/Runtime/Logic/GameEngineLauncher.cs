using GameEngine.Runtime.Logic.Procedure;
using GameEngine.Runtime.Base.Procedure;
using GameEngine.Runtime.Module;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using GameEngine.Runtime.Base.Launcher;

namespace GameEngine.Runtime.Logic
{
    internal class GameEngineLauncher: LauncherBase
    {
        protected void Awake()
        {
            DontDestroyOnLoad(this);
            Initialize(new ProcedureBase[]{
                new InitGlobalBlackboardProcedure(),
                new InitAssetModuleProcedure(),
                new InitEventModuleProcedure(),
                new InitTimerModuleProcedure(),
                new InitNetModuleProcedure(),
                new InitConfigModuleProcedure(),
                new InitAudioModuleProcedure(),
                new InitFinishedProcedure(),
            });
            EntranceProcedure = typeof(InitGlobalBlackboardProcedure);
        }

        protected void Start()
        {
            base.Start();
        }

        protected void Update()
        {
            base.Update();

            ModuleManager.Instance.Update(Time.deltaTime, Time.realtimeSinceStartup);
        }

        protected void FixedUpdate()
        {

        }

        protected void LateUpdate()
        {
            
        }

        protected void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
