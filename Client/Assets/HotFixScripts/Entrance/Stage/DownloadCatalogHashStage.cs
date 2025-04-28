using Entrance;
using System;
using System.Collections;
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

namespace Entrance.Stage
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
            _runner.StartCoroutine(DoTasks());
        }

        IEnumerator DoTasks()
        {
            yield return EntranceMgr.I.LoadLocalFile("com.unity.addressables/intermediate/catalog.json", (handler) =>
            {
                if (handler != null)
                {
                    EntranceMgr.I.localCatalogHash = handler.text;
                    Debug.Log($"LocalCatalogHash: {EntranceMgr.I.localCatalogHash}");
                }
            });

            yield return EntranceMgr.I.DownloadWithRetry(
                PathDefine.remoteCatalogHashUrl,
                null,
                3,
                10,
                (handler) =>
                {
                    if (handler != null)
                    {
                        Debug.Log($"Download CatalogHash Success, Hash：{handler.text}");
                        EntranceMgr.I.remoteCatalogHash = handler.text;
                    }
                    else
                    {
                        Debug.Log("Download CatalogHash Failed");
                    }
                });

            if (EntranceMgr.I.IsCatalogHashChanged())
            {
                Owner.ChangeStage<DownloadCatalogStage>();
            }
            else
            {
                Owner.ChangeStage<ReloadCatalogStage>();
            }
        }
    }
}
