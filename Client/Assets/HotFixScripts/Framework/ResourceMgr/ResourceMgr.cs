using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;


namespace Framework
{
    internal sealed class ResourceMgr : IResourceMgr, IFrameworkModule
    {
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
