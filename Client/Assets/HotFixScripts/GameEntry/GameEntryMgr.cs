using UnityEngine.Networking;
using UnityEngine;
using System.Collections;
using System;
using Framework;

namespace GameEntry
{
    internal partial class GameEntryMgr : Singleton<GameEntryMgr>
    {
        public static void Entry()
        {
            Debug.Log(" GameEntryMgr 进来了");
            var go = new GameObject("FW");
            GameObject.DontDestroyOnLoad(go);
            go.AddComponent<FW>();

            go = new GameObject("GameEntryStages");
            GameObject.DontDestroyOnLoad(go);
            go.AddComponent<GameEntryStages>();
        }

        public string remoteCatalogHash;
        public string localCatalogHash;


        public bool IsCatalogHashChanged()
        {
            if (string.IsNullOrEmpty(localCatalogHash) || string.IsNullOrEmpty(remoteCatalogHash))
                return false;

            return localCatalogHash.CompareTo(remoteCatalogHash) != 0;
        }

    }
}
