using Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace GameEntry.Stage
{
    internal class DownloadBundleStage : FsmState
    {
        MonoBehaviour _runner;
        public DownloadBundleStage(MonoBehaviour runner)
        {
            _runner = runner;
        }
        protected override void OnEnter()
        {
            _runner.StartCoroutine(DoTask());
        }

        IEnumerator DoTask()
        {
            yield return DownloadBundles();
            ChangeState<EntranceEndStage>();
        }

        IEnumerator DownloadBundles()
        {
            ulong totalSize = 0;
            var bundleLocations = new HashSet<IResourceLocation>();
            foreach (var loc in GameEntryMgr.I.AllLocations)
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
                    var size = (ulong)sizeData.ComputeSize(location, Addressables.ResourceManager);
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
                            var progressEvent = LoadingProgressEvent.Create(status.Percent, $"{Utility.FormatByteSize((ulong)status.DownloadedBytes)} / {Utility.FormatByteSize((ulong)status.TotalBytes)}");
                            FW.EventMgr.BroadcastAsync(progressEvent);
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
        }

        protected override void OnLeave()
        {
            FW.ResourceMgr.BundleDownloadCompleted -= OnDownloadCompleted;
        }


        void OnDownloadCompleted()
        {
            
        }
    }
}
