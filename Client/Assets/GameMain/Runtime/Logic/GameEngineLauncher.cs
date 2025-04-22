using GameMain.Runtime.Module;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GameMain.Runtime.Logic
{
    internal class GameEngineLauncher : MonoBehaviour
    {
        protected void Awake()
        {
           /* DontDestroyOnLoad(this);
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
            EntranceProcedure = typeof(InitGlobalBlackboardProcedure);*/
        }

        protected void Start()
        {

        }

        protected void Update()
        {


            ModuleManager.Instance.Update(Time.deltaTime, Time.realtimeSinceStartup);
        }

        protected void FixedUpdate()
        {
            ModuleManager.Instance.FixUpdate(Time.deltaTime, Time.realtimeSinceStartup);
        }

        protected void LateUpdate()
        {
            ModuleManager.Instance.LateUpdate(Time.deltaTime, Time.realtimeSinceStartup);
        }

        protected void OnDestroy()
        {

        }
    }
}
