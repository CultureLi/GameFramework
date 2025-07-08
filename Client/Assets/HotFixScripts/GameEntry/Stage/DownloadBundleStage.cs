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

        ulong _totalSize;
        List<IResourceLocation> _downloadLocations = new List<IResourceLocation>();
        public DownloadBundleStage(MonoBehaviour runner)
        {
            _runner = runner;
        }
        protected override void OnEnter()
        {
            CollectBundleInfo();
            Debug.Log($"Need download bundle Size: {Utility.FormatByteSize(_totalSize)}");
            if (_totalSize > 0)
            {
                var uiData = new GameEntryMsgBoxData()
                {
                    content = $"{Utility.FormatByteSize(_totalSize)}",
                    callback = (result) =>
                    {
                        if (result)
                        {
                            _runner.StartCoroutine(DoTask());
                        }
                        else
                        {
                            AppHelper.QuitGame();
                        }
                    }
                };
                FW.UIMgr.OpenUI("GameEntry/UIGameEntryMsgBox", 0, uiData);
            }
            else
            {
                ChangeState<DownloadConfigDataStage>();
            }
        }

        IEnumerator DoTask()
        {
            yield return DownloadBundles();
            ChangeState<DownloadConfigDataStage>();
        }

        void CollectBundleInfo()
        {
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

            foreach (var location in bundleLocations)
            {
                if (location.Data is ILocationSizeData sizeData)
                {
                    var size = (ulong)sizeData.ComputeSize(location, Addressables.ResourceManager);
                    if (size > 0)
                    {
                        _downloadLocations.Add(location);
                        Debug.Log($"Need download location：{location.PrimaryKey}");
                        _totalSize += size;
                    }
                }
            }
        }

        IEnumerator DownloadBundles()
        {
            var downloadHandle = Addressables.DownloadDependenciesAsync(_downloadLocations, false);

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

            Addressables.Release(downloadHandle);
            Debug.Log("All bundle download finished！");
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
