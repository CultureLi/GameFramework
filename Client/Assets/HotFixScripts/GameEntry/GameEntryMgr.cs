using UnityEngine.Networking;
using UnityEngine;
using System.Collections;
using System;
using Framework;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace GameEntry
{
    internal partial class GameEntryMgr : Singleton<GameEntryMgr>
    {
        public static void Entry()
        {
            Debug.Log(" GameEntryMgr Entry()");
            var go = new GameObject("FW");
            GameObject.DontDestroyOnLoad(go);
            go.AddComponent<FW>();

            go = new GameObject("GameEntryStages");
            GameObject.DontDestroyOnLoad(go);
            go.AddComponent<GameEntryStages>();
        }

        public string remoteCatalogHash;
        public string localCatalogHash;

        public AsyncOperationHandle<SceneInstance> LoadingSceneHandle;
        public HashSet<IResourceLocation> AllLocations = new HashSet<IResourceLocation>();

        public bool IsCatalogHashChanged()
        {
            if (string.IsNullOrEmpty(localCatalogHash) || string.IsNullOrEmpty(remoteCatalogHash))
                return false;

            return localCatalogHash.CompareTo(remoteCatalogHash) != 0;
        }

        

    }
}
