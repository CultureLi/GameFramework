using UnityEngine;
using Framework;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using Cysharp.Threading.Tasks;
using System;
using System.Linq;

namespace GameEntry
{
    public partial class GameEntryMgr : Singleton<GameEntryMgr>
    {
        public static async UniTaskVoid Entry()
        {
            FW.I.Initialize();
            AppConfig.Initialize();

            FW.UIMgr.CloseAll();
            Debug.Log("加载Login场景");
            //不能在completed回调中调用handler.WaitForCompletion(),会报错：
            ///Reentering the Update method is not allowed.  This can happen when calling WaitForCompletion on an operation while inside of a callback
            var handle = FW.ResMgr.LoadSceneAsync("Login");
            await handle.ToUniTask();

            await UniTask.NextFrame();
            new GameObject("GameEntryStages").AddComponent<GameEntryStages>();
        }

        internal Transform UIRoot
        {
            get;set;
        }

        internal string RemoteCatalogHash { get; set; }
        internal string LocalCatalogHash
        {
            get; set;
        }

        internal AsyncOperationHandle<SceneInstance> LoadingSceneHandle;
        internal HashSet<IResourceLocation> AllLocations = new HashSet<IResourceLocation>();

        // catalogHash是否发生了变化，决定是否需要进行资源热更
        internal bool IsCatalogHashChanged()
        {
            if (string.IsNullOrEmpty(LocalCatalogHash) || string.IsNullOrEmpty(RemoteCatalogHash))
                return false;

            return LocalCatalogHash.CompareTo(RemoteCatalogHash) != 0;
        }

        internal void EnterGameMain()
        {
            FW.UIMgr.CloseAll();

            var assembly = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "GameMain");

            if (assembly == null)
            {
                Debug.LogError("没有找到GameMain");
            }
            Type entry = assembly?.GetType("GameMain.GameMainEntryMgr");
            if (entry == null)
            {
                Debug.LogError("没有找到GameMain.GameMainEntry 入口");
            }
            entry?.GetMethod("Entry").Invoke(null, null);
        }
    }
}
