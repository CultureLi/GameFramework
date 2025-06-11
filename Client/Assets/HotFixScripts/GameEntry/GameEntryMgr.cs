using UnityEngine;
using Framework;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using Cysharp.Threading.Tasks;

namespace GameEntry
{
    internal partial class GameEntryMgr : Singleton<GameEntryMgr>
    {
        public static async UniTaskVoid Entry()
        {
            GameEntry.I.Initialize();
            Debug.Log("加载Login场景");
            //不能在completed回调中调用handler.WaitForCompletion(),会报错：
            ///Reentering the Update method is not allowed.  This can happen when calling WaitForCompletion on an operation while inside of a callback
            var handle = GameEntry.ResourceMgr.LoadSceneAsync("Assets/BundleRes/Scene/Login.unity");
            await handle.ToUniTask();

            await UniTask.NextFrame();
            new GameObject("GameEntryStages").AddComponent<GameEntryStages>();
        }

        public string RemoteCatalogHash { get; set; }
        public string LocalCatalogHash
        {
            get; set;
        }

        public AsyncOperationHandle<SceneInstance> LoadingSceneHandle;
        public HashSet<IResourceLocation> AllLocations = new HashSet<IResourceLocation>();

        // catalogHash是否发生了变化，决定是否需要进行资源热更
        public bool IsCatalogHashChanged()
        {
            if (string.IsNullOrEmpty(LocalCatalogHash) || string.IsNullOrEmpty(RemoteCatalogHash))
                return false;

            return LocalCatalogHash.CompareTo(RemoteCatalogHash) != 0;
        }
    }
}
