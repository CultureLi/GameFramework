using Framework;
using System.Collections;
using UnityEngine;

namespace GameEntry.Stage
{
    internal class DownloadCatalogHashStage : FsmState
    {
        MonoBehaviour _runner;
        public DownloadCatalogHashStage(MonoBehaviour runner)
        {
            _runner = runner;
        }

        protected override void OnEnter()
        {
            _runner.StartCoroutine(DoTasks());
        }

        IEnumerator DoTasks()
        {
            yield return FW.ResourceMgr.LoadLocalFile("com.unity.addressables/intermediate/catalog.json", (handler) =>
            {
                if (handler != null)
                {
                    GameEntryMgr.I.localCatalogHash = handler.text;
                    Debug.Log($"LocalCatalogHash: {GameEntryMgr.I.localCatalogHash}");
                }
            });

            yield return FW.ResourceMgr.LoadFile(PathDefine.remoteCatalogHashUrl,
                (handler) =>
                {
                    if (handler != null)
                    {
                        Debug.Log($"Download CatalogHash Success, Hash：{handler.text}");
                        GameEntryMgr.I.remoteCatalogHash = handler.text;
                    }
                    else
                    {
                        Debug.Log("Download CatalogHash Failed");
                    }
                });


            ChangeState<ReloadCatalogStage>();
        }
    }
}
