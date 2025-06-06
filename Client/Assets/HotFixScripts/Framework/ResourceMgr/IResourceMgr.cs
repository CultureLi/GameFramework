using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using static UnityEngine.AddressableAssets.Addressables;

namespace Framework
{
    public interface IResourceMgr
    {
        Action BundleDownloadStart
        {
            get; set;
        }

        Action BundleDownloadCompleted
        {
            get; set;
        }

        Action<DownloadStatus> BundleDownloadProgress
        {
            get; set;
        }

        IEnumerator DownloadRemoteFile(string path, Action<DownloadHandler> completedCb, int tryCount = 3, int timeout = 10);
        IEnumerator LoadLocalFile(string relativePath, Action<DownloadHandler> completedCb);
        IEnumerator LoadLocalFile(string[] paths, Action<DownloadHandler> completedCb);

        IEnumerator ReloadRemoteCatalog(string url, Action<IResourceLocator> completedCb = null);

        public void CollectRemoteResInfo(IResourceLocator localCatalog, IResourceLocator remoteCatalog);

        IEnumerator DownloadBundles();

        public AsyncOperationHandle<IResourceLocator> InitializeAsync();
        public AsyncOperationHandle<IResourceLocator> LoadContentCatalogAsync(string catalogPath);

        public AsyncOperationHandle<TObject> LoadAssetAsync<TObject>(IResourceLocation location);
        public AsyncOperationHandle<TObject> LoadAssetAsync<TObject>(object key);
        public AsyncOperationHandle<IList<TObject>> LoadAssetsAsync<TObject>(IList<IResourceLocation> locations, Action<TObject> callback);
        public AsyncOperationHandle<IList<TObject>> LoadAssetsAsync<TObject>(IEnumerable keys, Action<TObject> callback, MergeMode mode);
        public AsyncOperationHandle<IList<TObject>> LoadAssetsAsync<TObject>(object key, Action<TObject> callback);

        public AsyncOperationHandle<IList<IResourceLocation>> LoadResourceLocationsAsync(IEnumerable keys, MergeMode mode, Type type = null);
        public AsyncOperationHandle<IList<IResourceLocation>> LoadResourceLocationsAsync(object key, Type type = null);

        public void Release<TObject>(TObject obj);
        public void Release<TObject>(AsyncOperationHandle<TObject> handle);
        public void Release(AsyncOperationHandle handle);
        public bool ReleaseInstance(GameObject instance);
        public bool ReleaseInstance(AsyncOperationHandle handle);
        public bool ReleaseInstance(AsyncOperationHandle<GameObject> handle);

        public AsyncOperationHandle<long> GetDownloadSizeAsync(object key);
        public AsyncOperationHandle<long> GetDownloadSizeAsync(string key);
        public AsyncOperationHandle<long> GetDownloadSizeAsync(IEnumerable keys);

        public AsyncOperationHandle DownloadDependenciesAsync(object key, bool autoReleaseHandle = false);
        public AsyncOperationHandle DownloadDependenciesAsync(IList<IResourceLocation> locations, bool autoReleaseHandle = false);
        public AsyncOperationHandle DownloadDependenciesAsync(IEnumerable keys, MergeMode mode, bool autoReleaseHandle = false);

        public void ClearDependencyCacheAsync(object key);
        public void ClearDependencyCacheAsync(IList<IResourceLocation> locations);

        public void ClearDependencyCacheAsync(IEnumerable keys);
        public void ClearDependencyCacheAsync(string key);
        public AsyncOperationHandle<bool> ClearDependencyCacheAsync(object key, bool autoReleaseHandle);
        public AsyncOperationHandle<bool> ClearDependencyCacheAsync(IList<IResourceLocation> locations, bool autoReleaseHandle);
        public AsyncOperationHandle<bool> ClearDependencyCacheAsync(IEnumerable keys, bool autoReleaseHandle);
        public AsyncOperationHandle<bool> ClearDependencyCacheAsync(string key, bool autoReleaseHandle);
        public ResourceLocatorInfo GetLocatorInfo(string locatorId);
        public ResourceLocatorInfo GetLocatorInfo(IResourceLocator locator);

        public AsyncOperationHandle<GameObject> InstantiateAsync(IResourceLocation location, Transform parent = null, bool instantiateInWorldSpace = false, bool trackHandle = true);

        public AsyncOperationHandle<GameObject> InstantiateAsync(IResourceLocation location, Vector3 position, Quaternion rotation, Transform parent = null, bool trackHandle = true);

        public AsyncOperationHandle<GameObject> InstantiateAsync(object key, Transform parent = null, bool instantiateInWorldSpace = false, bool trackHandle = true);

        public AsyncOperationHandle<GameObject> InstantiateAsync(object key, Vector3 position, Quaternion rotation, Transform parent = null, bool trackHandle = true);

        public AsyncOperationHandle<GameObject> InstantiateAsync(object key, InstantiationParameters instantiateParameters, bool trackHandle = true);

        public AsyncOperationHandle<GameObject> InstantiateAsync(IResourceLocation location, InstantiationParameters instantiateParameters, bool trackHandle = true);

        public AsyncOperationHandle<SceneInstance> LoadSceneAsync(object key, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100);

        public AsyncOperationHandle<SceneInstance> LoadSceneAsync(object key, LoadSceneParameters loadSceneParameters, bool activateOnLoad = true, int priority = 100);

        public AsyncOperationHandle<SceneInstance> LoadSceneAsync(IResourceLocation location, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100);

        public AsyncOperationHandle<SceneInstance> LoadSceneAsync(IResourceLocation location, LoadSceneParameters loadSceneParameters, bool activateOnLoad = true, int priority = 100);

        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(SceneInstance scene, UnloadSceneOptions unloadOptions, bool autoReleaseHandle = true);

        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(AsyncOperationHandle handle, UnloadSceneOptions unloadOptions, bool autoReleaseHandle = true);

        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(SceneInstance scene, bool autoReleaseHandle = true);

        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(AsyncOperationHandle handle, bool autoReleaseHandle = true);

        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(AsyncOperationHandle<SceneInstance> handle, bool autoReleaseHandle = true);

        public AsyncOperationHandle<List<string>> CheckForCatalogUpdates(bool autoReleaseHandle = true);

        public AsyncOperationHandle<List<IResourceLocator>> UpdateCatalogs(IEnumerable<string> catalogs = null, bool autoReleaseHandle = true);

        public AsyncOperationHandle<List<IResourceLocator>>
            UpdateCatalogs(bool autoCleanBundleCache, IEnumerable<string> catalogs = null, bool autoReleaseHandle = true);

        public void RemoveResourceLocator(IResourceLocator locator);

        public void ClearResourceLocators();

        public AsyncOperationHandle<bool> CleanBundleCache(IEnumerable<string> catalogsIds = null);

    }
}
