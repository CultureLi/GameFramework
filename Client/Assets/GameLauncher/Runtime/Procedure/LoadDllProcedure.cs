using Cysharp.Threading.Tasks;
using GameEngine.Runtime.Base;
using GameEngine.Runtime.Base.Procedure;
using GameEngine.Runtime.Base.Utilitys;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR;
using YooAsset;

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
            // 补充元数据
            LoadMetadataForAOTAssemblies();

            // 加载热更dll
            var list = new []{"GameEngine.Runtime.Module.dll","GameEngine.Runtime.Logic.dll" ,"GameMain.Runtime.dll"};
            foreach (var name in list)
            {
                var handle = YooAssets.LoadAssetSync<TextAsset>(name);
                var bytes = (handle.AssetObject as TextAsset).bytes;
                var dll = Assembly.Load(bytes);

                Log.Info($"加载热更dll..{name}");
            }
#endif

            ChangeState<EndProcedure>();
        }

        private async void LoadMetadataForAOTAssemblies()
        {
            var list = new[] { "mscorlib.dll", "System.dll", "System.Core.dll" };
            foreach (var name in list)
            {
                var handle = YooAssets.LoadAssetSync<TextAsset>(name);
                var bytes = (handle.AssetObject as TextAsset).bytes;
                int err = (int)HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(bytes, HybridCLR.HomologousImageMode.SuperSet);
                Log.Info($"补充元数据:{name}. ret:{err}");

            }
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);
            
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

