using UnityEngine.Networking;
using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Linq;
using UnityEngine.ResourceManagement.ResourceProviders;
using System.IO;

namespace Launcher.Runtime
{
    internal partial class LauncherMgr
    {
        private static LauncherMgr instance;
        private static readonly object locker = new();

        public static LauncherMgr I
        {
            get
            {
                if (instance == null)
                {
                    lock (locker)
                    {
                        if (instance == null)
                        {
                            instance = new LauncherMgr();
                        }
                    }
                }
                return instance;
            }
        }


        public IResourceLocator ResourceLocator;
        //和本地安装时catalog相比下，远端bundle的location,用来做资源location重定向、资源下载大小计算
        Dictionary<string, string> _remoteBundlesLocationMap = new Dictionary<string, string>();

        // 记录所有（非Bundle）的location, 因为Addressables.DownloadDependenciesAsync()
        // 并不支持直接用 bundle 的 URL 当 key 来下载依赖资源
        public HashSet<IResourceLocation> _allLocations = new HashSet<IResourceLocation>();

        public bool isCatalogHashChanged = false;

        public Action ForceUpdateApp
        {
            get; set;
        }

        public Action BundleDownloadStart
        {
            get; set;
        }

        public Action BundleDownloadCompleted
        {
            get; set;
        }

        public Action<DownloadStatus> BundleDownloadStatus
        {
            get; set;
        }

        public static string ByteToMB(long bytes)
        {
            return ((double)bytes / 1048576).ToString("f2");
        }

        public IEnumerator DownloadWithRetry(string url, string savePath, int retryCount = 3, int timeoutSeconds = 10, Action<DownloadHandler> completedCb = null)
        {
            for (int i = 0; i < retryCount; i++)
            {
                using (UnityWebRequest uwr = UnityWebRequest.Get(url))
                {
                    // 设置超时时间（单位：秒）
                    uwr.timeout = timeoutSeconds;

                    // 如果你想写入文件
                    if (!string.IsNullOrEmpty(savePath))
                        uwr.downloadHandler = new DownloadHandlerFile(savePath, append: false);

                    Debug.Log($"🔄 第 {i + 1} 次尝试下载: {url}");

                    yield return uwr.SendWebRequest();

                    if (uwr.result == UnityWebRequest.Result.Success)
                    {
                        Debug.Log($"✅ 下载成功: {url}");
                        completedCb?.Invoke(uwr.downloadHandler);
                        yield break;
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ 下载失败: {url}，错误: {uwr.error}");
                        // 如果是最后一次也失败了
                        if (i == retryCount - 1)
                        {
                            Debug.LogError($"❌ 最终下载失败: {url}");
                            completedCb?.Invoke(null);
                        }
                    }
                }

                yield return new WaitForSeconds(1f); // 可以加一个延迟再重试
            }
        }


        void AddRemoteBundlesLocation(IResourceLocation location)
        {
            _remoteBundlesLocationMap[location.InternalId] = Path.Combine(PathDefine.remoteBundleUrl,
                Path.GetFileName(location.InternalId));
        }

        public void ReloadCatalog()
        {
            var locators = Addressables.ResourceLocators.ToList();
            IResourceLocator localLocator = locators.Find(e => e is ResourceLocationMap);

            var handle = Addressables.LoadContentCatalogAsync(PathDefine.newestCalalogPath);
            handle.WaitForCompletion();
            ResourceLocator = handle.Result;
            handle.Release();

            foreach (var locator in locators)
            {
                Addressables.RemoveResourceLocator(locator);
            }

            CollectRemoteResInfo(localLocator, ResourceLocator);
            Addressables.InternalIdTransformFunc = InternalIdTransform;
        }

        public void CollectRemoteResInfo(IResourceLocator localCatalog, IResourceLocator remoteCatalog)
        {
            _allLocations.Clear();
            foreach (var key in remoteCatalog.Keys)
            {
                if (!remoteCatalog.Locate(key, typeof(object), out var remoteLocations))
                    continue;
                foreach (var remoteLoc in remoteLocations)
                {
                    // 筛选出 AssetBundle 类型
                    if (remoteLoc.ResourceType != typeof(IAssetBundleResource))
                    {
                        _allLocations.Add(remoteLoc);
                        continue;
                    }
                    // 查看 local 是否也有
                    bool foundInLocal = localCatalog.Locate(key, typeof(object), out var localLocations);

                    if (!foundInLocal)
                    {
                        AddRemoteBundlesLocation(remoteLoc);
                        Debug.Log($"🆕 新资源: {key}");
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
                            AddRemoteBundlesLocation(remoteLoc);
                            Debug.Log($"🔁 资源更新: {key} | localHash = {localHash}, remoteHash = {remoteHash}");
                        }
                    }
                }
            }
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
                Debug.Log($"kk---{internalId}");
                return internalId;
            }

            return location.InternalId;
        }

        public IEnumerator DownloadBundles()
        {
            long totalSize = 0;
            var bundleLocations = new HashSet<IResourceLocation>();
            foreach (var loc in _allLocations)
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

            Debug.Log($"需要下载bundle Size: {ByteToMB(totalSize)}MB");

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
                            Debug.Log($"📦 下载进度: {ByteToMB(status.DownloadedBytes)}MB / {ByteToMB(status.TotalBytes)}MB ({status.Percent})");
                            BundleDownloadStatus?.Invoke(status);
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
    }
}
