using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Launcher.Runtime.Stage
{
    internal class LoadDllStage : StageBase
    {
        protected internal override void OnEnter()
        {

            //#if !UNITY_EDITOR
            // 补充元数据
            LoadMetadataForAOTAssemblies();

            // 加载热更dll
            var list = new []{"Logic.Runtime.dll"};
            foreach (var name in list)
            {
                var bytes = File.ReadAllBytes(Path.Combine(Application.streamingAssetsPath, name));
                var dll = Assembly.Load(bytes);

                Type entry = dll.GetType("GameMain.Runtime.Logic.GameMainEntry");
                var method = entry.GetMethod("Entry");
                method.Invoke(null, null);
                Debug.Log($"加载热更dll..{name}");
            }
            //#endif

            Owner.ChangeStage<LauncherEndStage>();
        }

        private void LoadMetadataForAOTAssemblies()
        {
            var list = new[] { "mscorlib.dll", "System.dll", "System.Core.dll" };
            foreach (var name in list)
            {
                var bytes = File.ReadAllBytes(Path.Combine(Application.streamingAssetsPath, name));
                int err = (int)HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(bytes, HybridCLR.HomologousImageMode.SuperSet);
                Debug.Log($"补充元数据:{name}. ret:{err}");

            }
        }
    }
}
