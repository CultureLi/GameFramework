using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;


namespace Framework
{
    internal sealed class ResourceMgr : IResourceMgr, IFramework
    {
        //和本地安装时catalog相比下，远端bundle的location,用来做资源location重定向、资源下载大小计算
        Dictionary<string, string> _remoteBundlesLocationMap = new Dictionary<string, string>();
        // 记录所有（非Bundle）的location, 因为Addressables.DownloadDependenciesAsync()
        // 并不支持直接用 bundle 的 URL 当 key 来下载依赖资源
        HashSet<IResourceLocation> _allLocations = new HashSet<IResourceLocation>();

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
        /// 加载本地、远端文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="completedCb"></param>
        /// <param name="tryCount">尝试次数</param>
        /// <param name="timeout">超时</param>
        /// <returns></returns>
        public IEnumerator DownloadRemoteFile(string path, Action<DownloadHandler> completedCb, int tryCount = 3, int timeout = 10)
        {
            for (int i = 0; i < tryCount; i++)
            {
                var uwr = UnityWebRequest.Get(path);
                uwr.timeout = timeout;
                yield return uwr.SendWebRequest();
                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        completedCb?.Invoke(uwr.downloadHandler);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                    yield break;
                }
                else
                {
                    if (i == tryCount - 1)
                    {
                        Debug.LogWarning($"Download failed: {path} error: {uwr.error}");
                    }
                    completedCb?.Invoke(null);
                }

                yield return new WaitForSeconds(1f);
            }
        }

        /// <summary>
        /// 更改bundle加载路径
        /// </summary>
        /// <param name="location"></param>
        public void ModifyBundleLocation(string internalId, string location)
        {
            _remoteBundlesLocationMap[internalId] = location;
        }

        /// <summary>
        /// 资源路径重定向
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        private string InternalIdTransform(IResourceLocation location)
        {
            if (_remoteBundlesLocationMap.TryGetValue(location.InternalId, out var internalId))
            {
                return internalId;
            }

            return location.InternalId;
        }

        public void SetInternalIdTransform(Func<IResourceLocation, string> func = null)
        {
            Addressables.InternalIdTransformFunc = func??InternalIdTransform;
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

        public AsyncOperationHandle<TObject> LoadAsset<TObject>(object key)
        {
            var op = LoadAssetAsync<TObject>(key);
            op.WaitForCompletion();
            return op;
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

        /// <summary>
        /// 加载场景
        /// </summary>
        /// <param name="path">对于"Assets/BundleRes/Scene"的相对路径</param>
        /// <param name="loadMode"></param>
        /// <param name="activateOnLoad"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public AsyncOperationHandle<SceneInstance> LoadSceneAsync(string path, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100)
        {
            var scenePath = $"Assets/BundleRes/Scene/{path}.unity";
            var oldScene = SceneManager.GetActiveScene();
            // 如果是single模式，会自动卸载之前的场景
            var handle = Addressables.LoadSceneAsync(scenePath, loadMode, activateOnLoad, priority);

            if (loadMode == LoadSceneMode.Additive)
            {
                handle.AddCompleted(_ =>
                {
                    var newScene = handle.Result.Scene;
                    handle.Result.ActivateAsync().AddCompleted(_ =>
                    {
                        SceneManager.SetActiveScene(newScene);
                    });

                    UnloadSceneAsync(oldScene);
                });
            }

            return handle;
        }

        AsyncOperation UnloadSceneAsync(Scene scene)
        {
            var handle = SceneManager.UnloadSceneAsync(scene);
            handle.AddCompleted((_) =>
            {
                Resources.UnloadUnusedAssets();
                GC.Collect();
            });

            return handle;
        }

        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(SceneInstance instance)
        {
            var handle = Addressables.UnloadSceneAsync(instance, true);
            handle.AddCompleted((_) =>
            {
                Resources.UnloadUnusedAssets();
                GC.Collect();
            });
            return handle;
        }

        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(AsyncOperationHandle<SceneInstance> handle)
        {
            return UnloadSceneAsync(handle.Result);
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
