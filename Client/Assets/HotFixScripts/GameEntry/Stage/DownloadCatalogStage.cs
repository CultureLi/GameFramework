using AOTBase;
using Framework;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;

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
            var hashPaths = new string[]
            {
                PathDefine.persistentCatalogPath,
                PathDefine.originCatalogHashPath,
            };

            //加载本地最新的hash
            yield return FW.ResourceMgr.LoadLocalFile(hashPaths, (handler) =>
            {
                if (handler != null)
                {
                    GameEntryMgr.I.localCatalogHash = handler.text;
                    Debug.Log($"LocalCatalogHash: {GameEntryMgr.I.localCatalogHash}");
                }
            });

            //请求远端hash
            yield return FW.ResourceMgr.DownloadRemoteFile(PathDefine.remoteCatalogHashUrl,
                (handler) =>
                {
                    if (handler != null)
                    {
                        Debug.Log($"Download CatalogHash Success");
                        GameEntryMgr.I.remoteCatalogHash = handler.text;
                    }
                    else
                    {
                        Debug.Log("Download CatalogHash Failed");
                    }
                });

            Debug.Log($"local:{GameEntryMgr.I.localCatalogHash} remote:{GameEntryMgr.I.remoteCatalogHash}");
            if (GameEntryMgr.I.IsCatalogHashChanged())
            {
                Debug.Log("ReloadRemoteCatalog");
                //加载远端catalog
                yield return FW.ResourceMgr.ReloadRemoteCatalog(PathDefine.remoteBundleUrl, (catalog) =>
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
                    yield return FW.ResourceMgr.ReloadRemoteCatalog(PathDefine.persistentCatalogPath, (catalog) =>
                    {
                        
                    });
                }

                ChangeState<EntranceEndStage>();
            }
        }
    }
}
