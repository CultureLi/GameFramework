using GameEngine.Runtime.Base;
using GameEngine.Runtime.Base.Procedure;
using GameEngine.Runtime.Base.Utilitys;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
namespace GameLauncher.Runtime.Procedure
{
    public class LoadDllProcedure : ProcedureBase
    {
        protected override void OnInit()
        {
            base.OnInit();
        }

        protected override void OnEnter()
        {
            base.OnEnter();
#if !UNITY_EDITOR
            // 先补充元数据
            LoadMetadataForAOTAssemblies();
            // 加载热更dll
            Assembly.Load(File.ReadAllBytes($"{Application.streamingAssetsPath}/GameEngine.Runtime.Module.dll.bytes"));
            Assembly.Load(File.ReadAllBytes($"{Application.streamingAssetsPath}/GameEngine.Runtime.Logic.dll.bytes"));
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

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);
            ChangeState<EndProcedure>();
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

