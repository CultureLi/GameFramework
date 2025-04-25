using Launcher.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Launcher.Runtime.Stage
{
    internal class DownloadCatalogHashStage : StageBase
    {
        MonoBehaviour _runner;
        public DownloadCatalogHashStage(MonoBehaviour runner)
        {
            _runner = runner;
        }

        protected internal override void OnEnter()
        {
            LauncherMgr.I.isCatalogHashChanged = false;
            _runner.StartCoroutine(LauncherMgr.I.DownloadWithRetry(
                PathDefine.remoteCatalogHashUrl,
                PathDefine.tempCalalogHashPath,
                3,
                10,
                OnDownloadCompleted));
        }

        void OnDownloadCompleted(DownloadHandler handler)
        {
            var localPath = PathDefine.originCatalogHashPath;

            if (File.Exists(PathDefine.newestCalalogHashPath))
            {
                localPath = PathDefine.newestCalalogHashPath;
            }

            var location = new ResourceLocationBase(localPath, localPath, typeof(TextDataProvider).FullName, typeof(string));
            location.Data = new ProviderLoadRequestOptions()
            {
                IgnoreFailures = true
            };

            var handle = Addressables.ResourceManager.ProvideResource<string>(location);
            handle.WaitForCompletion();
            var localHash = handle.Result;
            Addressables.Release(handle);


            //var remoteHash = File.ReadAllText(GlobaPath.tempCalalogHashPath);
            var remoteHash = handler.text;
            Debug.Log($"CatalogHash--> local: {localHash} remote: {remoteHash}");

            if (localHash != remoteHash)
            {
                LauncherMgr.I.isCatalogHashChanged = true;
                Owner.ChangeStage<DownloadCatalogStage>();
            }
            else
            {
                Owner.ChangeStage<ReloadCatalogStage>();
            }

        }
    }
}
