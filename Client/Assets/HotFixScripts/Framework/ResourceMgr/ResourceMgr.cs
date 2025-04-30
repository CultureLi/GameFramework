using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;


namespace Framework
{
    internal sealed class ResourceMgr : IResourceMgr, IFrameworkModule
    {
        string remoteBundleUrl;
        //和本地安装时catalog相比下，远端bundle的location,用来做资源location重定向、资源下载大小计算
        Dictionary<string, string> remoteBundlesLocationMap = new Dictionary<string, string>();
        // 记录所有（非Bundle）的location, 因为Addressables.DownloadDependenciesAsync()
        // 并不支持直接用 bundle 的 URL 当 key 来下载依赖资源
        public HashSet<IResourceLocation> allLocations = new HashSet<IResourceLocation>();

        public Action BundleDownloadStart
        {
            get; set;
        }

        public Action BundleDownloadCompleted
        {
            get; set;
        }

        public Action<DownloadStatus> BundleDownloadProgress
        {
            get; set;
        }


        /// <summary>
        /// 加载本地文件
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="completedCb"></param>
        /// <returns></returns>
        public IEnumerator LoadLocalFile(string relativePath, Action<DownloadHandler> completedCb)
        {
            string[] rootPaths = { Application.persistentDataPath, Application.streamingAssetsPath };
            foreach (var path in rootPaths)
            {
                var url = Path.Combine(path, relativePath);
                var uwr = UnityWebRequest.Get(url);
                yield return uwr.SendWebRequest();
                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    completedCb?.Invoke(uwr.downloadHandler);
                    yield break;
                }
                else
                {
                    completedCb?.Invoke(null);
                }
            }
        }

        /// <summary>
        /// 加载本地、远端文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="completedCb"></param>
        /// <param name="tryCount">尝试次数</param>
        /// <param name="timeout">超时</param>
        /// <returns></returns>
        public IEnumerator LoadFile(string path, Action<DownloadHandler> completedCb, int tryCount = 3, int timeout = 10)
        {
            for (int i = 0; i < tryCount; i++)
            {
                var uwr = UnityWebRequest.Get(path);
                uwr.timeout = timeout;
                yield return uwr.SendWebRequest();
                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    completedCb?.Invoke(uwr.downloadHandler);
                    yield break;
                }
                else
                {
                    completedCb?.Invoke(null);
                }

                yield return new WaitForSeconds(1f);
            }
        }

        /// <summary>
        /// 更改bundle加载路径
        /// </summary>
        /// <param name="location"></param>
        private void ModifyBundleLocation(IResourceLocation location)
        {
            remoteBundlesLocationMap[location.InternalId] = Path.Combine(remoteBundleUrl,
                Path.GetFileName(location.InternalId));
        }

        /// <summary>
        /// 资源路径重定向
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        private string InternalIdTransform(IResourceLocation location)
        {
            if (remoteBundlesLocationMap.TryGetValue(location.InternalId, out var internalId))
            {
                return internalId;
            }

            return location.InternalId;
        }

        /// <summary>
        /// 加载远端Catalog
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public IEnumerator ReloadRemoteCatalog(string url, Action completedCb)
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
            Addressables.InternalIdTransformFunc = InternalIdTransform;

            completedCb?.Invoke();
        }

        public void CollectRemoteResInfo(IResourceLocator localCatalog, IResourceLocator remoteCatalog)
        {
            allLocations.Clear();
            foreach (var key in remoteCatalog.Keys)
            {
                if (!remoteCatalog.Locate(key, typeof(object), out var remoteLocations))
                    continue;
                foreach (var remoteLoc in remoteLocations)
                {
                    // 筛选出 AssetBundle 类型
                    if (remoteLoc.ResourceType != typeof(IAssetBundleResource))
                    {
                        allLocations.Add(remoteLoc);
                        continue;
                    }
                    // 查看 local 是否也有
                    bool foundInLocal = localCatalog.Locate(key, typeof(object), out var localLocations);

                    if (!foundInLocal)
                    {
                        ModifyBundleLocation(remoteLoc);
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
                            ModifyBundleLocation(remoteLoc);
                        }
                    }
                }
            }
        }

        public IEnumerator DownloadBundles()
        {
            long totalSize = 0;
            var bundleLocations = new HashSet<IResourceLocation>();
            foreach (var loc in allLocations)
            {
                if (loc.HasDependencies)
                {
                    foreach (var dep in loc.Dependencies)
                    {
                        bundleLocations.Add(dep);
                    }
                }
            }

            var downloadLocations = new List<IResourceLocation>();
            foreach (var location in bundleLocations)
            {
                if (location.Data is ILocationSizeData sizeData)
                {
                    var size = sizeData.ComputeSize(location, Addressables.ResourceManager);
                    if (size > 0)
                    {
                        downloadLocations.Add(location);
                        Debug.Log($"需要下载：{location.PrimaryKey}");
                        totalSize += size;
                    }
                }
            }

            Debug.Log($"需要下载bundle Size: {Utility.FormatByteSize(totalSize)}");

            if (totalSize > 0)
            {
                BundleDownloadStart?.Invoke();

                var downloadHandle = Addressables.DownloadDependenciesAsync(downloadLocations, false);

                yield return null;

                var remainingTime = 0f;
                while (!downloadHandle.IsDone)
                {
                    remainingTime -= Time.deltaTime;

                    if (remainingTime <= 0f)
                    {
                        remainingTime = .3f;

                        var status = downloadHandle.GetDownloadStatus();
                        if (status.TotalBytes > 0) // 加一层判断
                        {
                            Debug.Log($"📦 下载进度: {Utility.FormatByteSize(status.DownloadedBytes)} / {Utility.FormatByteSize(status.TotalBytes)} ({status.Percent})");
                            BundleDownloadProgress?.Invoke(status);
                        }
                    }

                    yield return null;
                }
                //UpdateStatus();
                Addressables.Release(downloadHandle);
                Debug.Log("🎉 所有 bundle 下载完成！");
            }
            else
            {
                Debug.Log("不需要下载任何资源！");
            }
            BundleDownloadCompleted?.Invoke();
        }

        public AsyncOperationHandle<List<string>> CheckForCatalogUpdates(bool autoReleaseHandle = true)
        {
            return Addressables.CheckForCatalogUpdates(autoReleaseHandle);
        }

        public AsyncOperationHandle<bool> CleanBundleCache(IEnumerable<string> catalogsIds = null)
        {
            return Addressables.CleanBundleCache(catalogsIds);
        }

        public void ClearDependencyCacheAsync(object key)
        {
            Addressables.ClearDependencyCacheAsync(key);
        }

        public void ClearDependencyCacheAsync(IList<IResourceLocation> locations)
        {
            Addressables.ClearDependencyCacheAsync(locations);
        }

        public void ClearDependencyCacheAsync(IEnumerable keys)
        {
            Addressables.ClearDependencyCacheAsync(keys);
        }

        public void ClearDependencyCacheAsync(string key)
        {
            Addressables.ClearDependencyCacheAsync(key);
        }

        public AsyncOperationHandle<bool> ClearDependencyCacheAsync(object key, bool autoReleaseHandle)
        {
            return Addressables.ClearDependencyCacheAsync(key, autoReleaseHandle);
        }

        public AsyncOperationHandle<bool> ClearDependencyCacheAsync(IList<IResourceLocation> locations, bool autoReleaseHandle)
        {
            return Addressables.ClearDependencyCacheAsync(locations, autoReleaseHandle);
        }

        public AsyncOperationHandle<bool> ClearDependencyCacheAsync(IEnumerable keys, bool autoReleaseHandle)
        {
            return Addressables.ClearDependencyCacheAsync(keys, autoReleaseHandle);
        }

        public AsyncOperationHandle<bool> ClearDependencyCacheAsync(string key, bool autoReleaseHandle)
        {
            return Addressables.ClearDependencyCacheAsync(key, autoReleaseHandle);
        }

        public void ClearResourceLocators()
        {
            Addressables.ClearResourceLocators();
        }

        public AsyncOperationHandle DownloadDependenciesAsync(object key, bool autoReleaseHandle = false)
        {
            return Addressables.DownloadDependenciesAsync(key, autoReleaseHandle);
        }

        public AsyncOperationHandle DownloadDependenciesAsync(IList<IResourceLocation> locations, bool autoReleaseHandle = false)
        {
            return Addressables.DownloadDependenciesAsync(locations, autoReleaseHandle);
        }

        public AsyncOperationHandle DownloadDependenciesAsync(IEnumerable keys, Addressables.MergeMode mode, bool autoReleaseHandle = false)
        {
            return Addressables.DownloadDependenciesAsync(keys, autoReleaseHandle);
        }

        public AsyncOperationHandle<long> GetDownloadSizeAsync(object key)
        {
            return Addressables.GetDownloadSizeAsync(key);
        }

        public AsyncOperationHandle<long> GetDownloadSizeAsync(string key)
        {
            return Addressables.GetDownloadSizeAsync(key);
        }

        public AsyncOperationHandle<long> GetDownloadSizeAsync(IEnumerable keys)
        {
            return Addressables.GetDownloadSizeAsync(keys);
        }

        public ResourceLocatorInfo GetLocatorInfo(string locatorId)
        {
            return Addressables.GetLocatorInfo(locatorId);
        }

        public ResourceLocatorInfo GetLocatorInfo(IResourceLocator locator)
        {
            return Addressables.GetLocatorInfo(locator);
        }

        public AsyncOperationHandle<IResourceLocator> InitializeAsync()
        {
            return Addressables.InitializeAsync();
        }

        public AsyncOperationHandle<GameObject> InstantiateAsync(IResourceLocation location, Transform parent = null, bool instantiateInWorldSpace = false, bool trackHandle = true)
        {
            return Addressables.InstantiateAsync(location, parent, instantiateInWorldSpace, trackHandle);
        }

        public AsyncOperationHandle<GameObject> InstantiateAsync(IResourceLocation location, Vector3 position, Quaternion rotation, Transform parent = null, bool trackHandle = true)
        {
            return Addressables.InstantiateAsync(location, position, rotation, parent, trackHandle);
        }

        public AsyncOperationHandle<GameObject> InstantiateAsync(object key, Transform parent = null, bool instantiateInWorldSpace = false, bool trackHandle = true)
        {
            return Addressables.InstantiateAsync(key, parent, instantiateInWorldSpace, trackHandle);
        }

        public AsyncOperationHandle<GameObject> InstantiateAsync(object key, Vector3 position, Quaternion rotation, Transform parent = null, bool trackHandle = true)
        {
            return Addressables.InstantiateAsync(key, position, rotation, parent, trackHandle);
        }

        public AsyncOperationHandle<GameObject> InstantiateAsync(object key, InstantiationParameters instantiateParameters, bool trackHandle = true)
        {
            return Addressables.InstantiateAsync(key, instantiateParameters, trackHandle);
        }

        public AsyncOperationHandle<GameObject> InstantiateAsync(IResourceLocation location, InstantiationParameters instantiateParameters, bool trackHandle = true)
        {
            return Addressables.InstantiateAsync( location, instantiateParameters, trackHandle);
        }

        public AsyncOperationHandle<TObject> LoadAssetAsync<TObject>(IResourceLocation location)
        {
            return Addressables.LoadAssetAsync<TObject>(location);
        }

        public AsyncOperationHandle<TObject> LoadAssetAsync<TObject>(object key)
        {
            return Addressables.LoadAssetAsync<TObject>(key);
        }

        public AsyncOperationHandle<IList<TObject>> LoadAssetsAsync<TObject>(IList<IResourceLocation> locations, Action<TObject> callback)
        {
            return Addressables.LoadAssetsAsync<TObject>(locations, callback);
        }

        public AsyncOperationHandle<IList<TObject>> LoadAssetsAsync<TObject>(IEnumerable keys, Action<TObject> callback, Addressables.MergeMode mode)
        {
            return Addressables.LoadAssetsAsync<TObject>(keys, callback, mode);
        }

        public AsyncOperationHandle<IList<TObject>> LoadAssetsAsync<TObject>(object key, Action<TObject> callback)
        {
            return Addressables.LoadAssetsAsync<TObject>(key, callback);
        }

        public AsyncOperationHandle<IResourceLocator> LoadContentCatalogAsync(string catalogPath)
        {
            return Addressables.LoadContentCatalogAsync(catalogPath);
        }

        public AsyncOperationHandle<IList<IResourceLocation>> LoadResourceLocationsAsync(IEnumerable keys, Addressables.MergeMode mode, Type type = null)
        {
            return Addressables.LoadResourceLocationsAsync(keys, mode, type);
        }

        public AsyncOperationHandle<IList<IResourceLocation>> LoadResourceLocationsAsync(object key, Type type = null)
        {
            return Addressables.LoadResourceLocationsAsync(key, type);
        }

        public AsyncOperationHandle<SceneInstance> LoadSceneAsync(object key, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100)
        {
            return Addressables.LoadSceneAsync(key, loadMode, activateOnLoad, priority);
        }

        public AsyncOperationHandle<SceneInstance> LoadSceneAsync(object key, LoadSceneParameters loadSceneParameters, bool activateOnLoad = true, int priority = 100)
        {
            return Addressables.LoadSceneAsync(key, loadSceneParameters, activateOnLoad, priority);
        }

        public AsyncOperationHandle<SceneInstance> LoadSceneAsync(IResourceLocation location, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100)
        {
            return Addressables.LoadSceneAsync(location, loadMode, activateOnLoad, priority);
        }

        public AsyncOperationHandle<SceneInstance> LoadSceneAsync(IResourceLocation location, LoadSceneParameters loadSceneParameters, bool activateOnLoad = true, int priority = 100)
        {
            return Addressables.LoadSceneAsync(location, loadSceneParameters, activateOnLoad, priority);
        }

        public void Release<TObject>(TObject obj)
        {
            Addressables.Release(obj);
        }

        public void Release<TObject>(AsyncOperationHandle<TObject> handle)
        {
            Addressables.Release(handle);
        }

        public void Release(AsyncOperationHandle handle)
        {
            Addressables.Release(handle);
        }

        public bool ReleaseInstance(GameObject instance)
        {
            return Addressables.ReleaseInstance(instance);
        }

        public bool ReleaseInstance(AsyncOperationHandle handle)
        {
            return Addressables.ReleaseInstance(handle);
        }

        public bool ReleaseInstance(AsyncOperationHandle<GameObject> handle)
        {
            return Addressables.ReleaseInstance(handle);
        }

        public void RemoveResourceLocator(IResourceLocator locator)
        {
            Addressables.RemoveResourceLocator(locator);
        }

        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(SceneInstance scene, UnloadSceneOptions unloadOptions, bool autoReleaseHandle = true)
        {
            return Addressables.UnloadSceneAsync(scene, unloadOptions, autoReleaseHandle);
        }

        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(AsyncOperationHandle handle, UnloadSceneOptions unloadOptions, bool autoReleaseHandle = true)
        {
            return Addressables.UnloadSceneAsync(handle, unloadOptions, autoReleaseHandle);
        }

        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(SceneInstance scene, bool autoReleaseHandle = true)
        {
            return Addressables.UnloadSceneAsync(scene, autoReleaseHandle);
        }

        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(AsyncOperationHandle handle, bool autoReleaseHandle = true)
        {
            return Addressables.UnloadSceneAsync(handle, autoReleaseHandle);
        }

        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(AsyncOperationHandle<SceneInstance> handle, bool autoReleaseHandle = true)
        {
            return Addressables.UnloadSceneAsync(handle, autoReleaseHandle);
        }

        public AsyncOperationHandle<List<IResourceLocator>> UpdateCatalogs(IEnumerable<string> catalogs = null, bool autoReleaseHandle = true)
        {
            return Addressables.UpdateCatalogs(catalogs, autoReleaseHandle);
        }

        public AsyncOperationHandle<List<IResourceLocator>> UpdateCatalogs(bool autoCleanBundleCache, IEnumerable<string> catalogs = null, bool autoReleaseHandle = true)
        {
            return Addressables.UpdateCatalogs(autoReleaseHandle, catalogs, autoCleanBundleCache);
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {

        }

        public void Shutdown()
        {

        }
    }
}
