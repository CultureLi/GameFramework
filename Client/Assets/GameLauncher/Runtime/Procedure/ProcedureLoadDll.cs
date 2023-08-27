using GameEngine.Runtime.Base;
using GameEngine.Runtime.Base.Procedure;
using GameEngine.Runtime.Base.Utilitys;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using ProcedureOwner = GameEngine.Runtime.Fsm.IFsm<GameEngine.Runtime.Base.Procedure.IProcedureManager>;
namespace GameLauncher.Runtime.Base.Procedure
{
    public class ProcedureLoadDll : ProcedureBase
    {
        protected override void OnInit(ProcedureOwner procedureOwner)
        {
            base.OnInit(procedureOwner);
        }

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);
#if !UNITY_EDITOR
            // 先补充元数据
            LoadMetadataForAOTAssemblies();
            //Assembly.Load(File.ReadAllBytes($"{Application.streamingAssetsPath}/GameEngine.Runtime.Base.dll.bytes"));
            Assembly.Load(File.ReadAllBytes($"{Application.streamingAssetsPath}/GameEngine.Runtime.Module.dll.bytes"));
            //Assembly.Load(File.ReadAllBytes($"{Application.streamingAssetsPath}/GameEngine.Runtime.ThirdPart.dll.bytes"));
            Assembly.Load(File.ReadAllBytes($"{Application.streamingAssetsPath}/GameMain.Runtime.dll.bytes"));
#endif
        }

        private void LoadMetadataForAOTAssemblies()
        {
            List<string> aotDllList = new List<string>
        {
            "mscorlib.dll",
            "System.dll",
            "System.Core.dll", // 如果使用了Linq，需要这个
            // "Newtonsoft.Json.dll", 
            // "protobuf-net.dll",
        };

            foreach (var aotDllName in aotDllList)
            {
                byte[] dllBytes = File.ReadAllBytes($"{Application.streamingAssetsPath}/{aotDllName}.bytes");
                int err = (int)HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, HybridCLR.HomologousImageMode.SuperSet);
                Log.Debug($"LoadMetadataForAOTAssembly:{aotDllName}. ret:{err}");
            }
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
            ChangeState<ProcedureFinished>(procedureOwner);
        }


        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            base.OnLeave(procedureOwner, isShutdown);
        }

        protected override void OnDestroy(ProcedureOwner procedureOwner)
        {
            base.OnDestroy(procedureOwner);
        }


        private void InitLanguageSettings()
        {
            
        }
    }
}

