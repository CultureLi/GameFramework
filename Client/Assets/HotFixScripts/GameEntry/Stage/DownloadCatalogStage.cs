using AOTBase;
using Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace GameEntry.Stage
{
    internal class DownloadCatalogStage : FsmState
    {
        MonoBehaviour _runner;
        public DownloadCatalogStage(MonoBehaviour runner)
        {
            _runner = runner;
        }

        protected override void OnEnter()
        {
            _runner.StartCoroutine(DoTasks());
        }

        IEnumerator DoTasks()
        {
            yield return LoadLocalHash();
            yield return DownloadRemoteHash();
            yield return ReloadCatalog();
        }

        IEnumerator LoadLocalHash()
        {
            var hashPaths = new string[]{
                PathDefine.persistentCatalogPath,
                PathDefine.originCatalogHashPath,};

            //加载本地最新的hash
            yield return FW.ResourceMgr.LoadLocalFile(hashPaths, (handler) =>
            {
                if (handler != null)
                {
                    GameEntryMgr.I.LocalCatalogHash = handler.text;
                    Debug.Log($"LocalCatalogHash: {GameEntryMgr.I.LocalCatalogHash}");
                }
            });
        }

        IEnumerator DownloadRemoteHash()
        {
            //请求远端hash
            yield return FW.ResourceMgr.DownloadRemoteFile(PathDefine.remoteCatalogHashUrl,
                (handler) =>
                {
                    if (handler != null)
                    {
                        Debug.Log($"Download CatalogHash Success");
                        GameEntryMgr.I.RemoteCatalogHash = handler.text;
                    }
                    else
                    {
                        Debug.Log("Download CatalogHash Failed");
                    }
                });
        }

        IEnumerator ReloadCatalog()
        {
            Debug.Log($"local:{GameEntryMgr.I.LocalCatalogHash} remote:{GameEntryMgr.I.RemoteCatalogHash}");

            if (GameEntryMgr.I.IsCatalogHashChanged())
            {
                Debug.Log("ReloadRemoteCatalog");
                //加载远端catalog
                yield return ReloadRemoteCatalog(PathDefine.remoteCatalogUrl, (catalog) =>
                {
                    var remoteCatalog = catalog as ResourceLocationMap;
                    File.WriteAllText(PathDefine.persistentCatalogPath, remoteCatalog.ToString());
                    ChangeState<DownloadBundleStage>();
                });
            }
            else
            {
                Debug.Log("ReloadLocalCatalog");
                if (File.Exists(PathDefine.persistentCatalogPath))
                {
                    //加载本地catalog, 这里是沙盒目录中的
                    yield return ReloadRemoteCatalog(PathDefine.persistentCatalogPath, (catalog) =>
                    {
                        ChangeState<DownloadConfigDataStage>();
                    });
                }
                else
                {
                    ChangeState<DownloadConfigDataStage>();
                }
            }
        }

        /// <summary>
        /// 加载远端Catalog
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        IEnumerator ReloadRemoteCatalog(string url, Action<IResourceLocator> completedCb = null)
        {
            var oldLocators = Addressables.ResourceLocators.ToList();
            IResourceLocator localLocator = oldLocators.Find(e => e is ResourceLocationMap);

            var handle = Addressables.LoadContentCatalogAsync(url);
            yield return handle;
            var remoteLocator = handle.Result;
            handle.Release();

            foreach (var locator in oldLocators)
            {
                Addressables.RemoveResourceLocator(locator);
            }

            CollectRemoteResInfo(localLocator, remoteLocator);
            FW.ResourceMgr.SetInternalIdTransform();

            completedCb?.Invoke(remoteLocator);
        }

        void ModifyLocation(IResourceLocation location)
        {
            FW.ResourceMgr.ModifyBundleLocation(location.InternalId, Path.Combine(PathDefine.remoteBundleUrl, Path.GetFileName(location.InternalId)));
        }

        void CollectRemoteResInfo(IResourceLocator localCatalog, IResourceLocator remoteCatalog)
        {
            GameEntryMgr.I.AllLocations.Clear();
            foreach (var key in remoteCatalog.Keys)
            {
                if (!remoteCatalog.Locate(key, typeof(object), out var remoteLocations))
                    continue;
                foreach (var remoteLoc in remoteLocations)
                {
                    // 筛选出 AssetBundle 类型
                    if (remoteLoc.ResourceType != typeof(IAssetBundleResource))
                    {
                        GameEntryMgr.I.AllLocations.Add(remoteLoc);
                        continue;
                    }
                    // 查看 local 是否也有
                    bool foundInLocal = localCatalog.Locate(key, typeof(object), out var localLocations);

                    if (!foundInLocal)
                    {
                        ModifyLocation(remoteLoc);
                    }
                    else
                    {
                        var localLoc = localLocations[0]; // 默认只有一个
                        var localOptions = localLoc.Data as AssetBundleRequestOptions;
                        var remoteOptions = remoteLoc.Data as AssetBundleRequestOptions;

                        // 检查 hash 是否不同
                        string localHash = localOptions?.Hash ?? null;
                        string remoteHash = remoteOptions?.Hash ?? null;

                        if (localHash != remoteHash)
                        {
                            ModifyLocation(remoteLoc);
                        }
                    }
                }
            }
        }
    }
}
