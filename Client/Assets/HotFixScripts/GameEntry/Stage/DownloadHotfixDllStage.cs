using AOTBase;
using Framework;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace GameEntry.Stage
{
    // 在Hybridclr setting面板中HotUpdateAssemblyDefinitions中添加热更程序集
    // 在打包时，Hybridclr会自动将这些程序集生成的dll从主程序中剔除
    // 所以打初始包时，需要将这些dll放在StreamingAssets中作为初始dll
    // 通过远端manifest和本地manifest对比，下载最新dll
    // 根据manifest中依赖关系，按顺序依次加载dll, 查找dll顺序为先PersistentPath,后StreamingAssets

    internal class DownloadHotfixDllStage : FsmState
    {
        MonoBehaviour _runner;
        HotFixDllManifest _localManifest;
        HotFixDllManifest _remoteManifest;
        string _remoteManifestJsonText;
        ulong _totalSize;
        //是否需要重启
        bool needRestart;

        public DownloadHotfixDllStage(MonoBehaviour runner)
        {
            _runner = runner;
        }
        protected override void OnEnter()
        {
            _runner.StartCoroutine(DoHotFixTasks());
        }

        IEnumerator DoHotFixTasks()
        {
            yield return LoadLocalManifest();
            yield return DownloadRemoteManifest();
            yield return DownloadHotFixDlls();

            if (needRestart)
            {
                Debug.Log("需要重启");
            }
            else
            {
                ChangeState<DownloadCatalogStage>();
            }
        }

        /// <summary>
        /// 加载本地manifest
        /// </summary>
        /// <returns></returns>
        IEnumerator LoadLocalManifest()
        {
            Debug.Log("load local manifest");
            yield return FW.ResourceMgr.LoadLocalFile("hotFixDllManifest.json", (handler) =>
            {
                if (handler != null)
                {
                    _localManifest = JsonUtility.FromJson<HotFixDllManifest>(handler.text);
                }
            });
        }

        /// <summary>
        /// 下载远端manifest
        /// </summary>
        /// <returns></returns>
        IEnumerator DownloadRemoteManifest()
        {
            Debug.Log("download manifest start....");
            var url = PathDefine.remoteHotFixDllManifest;
            yield return FW.ResourceMgr.DownloadRemoteFile(url, (handler) =>
            {
                if (handler != null)
                {
                    Debug.Log("download manifest success!!!");
                    _remoteManifestJsonText = handler.text;
                    _remoteManifest = JsonUtility.FromJson<HotFixDllManifest>(_remoteManifestJsonText);
                }
            });
        }

        /// <summary>
        /// 对比hash，收集要下载的dll
        /// </summary>
        List<DllInfo> CollectNeedDownloadDlls()
        {
            Debug.Log("CollectNeedDownloadDlls");
            var needDownloadDlls = new List<DllInfo>();

            if (_remoteManifest == null || _localManifest == null)
                return needDownloadDlls;

/*            if (_remoteManifest.version.CompareTo(_localManifest.version) <= 0)
                return needDownloadDlls;*/

            foreach (var remoteDll in _remoteManifest.item)
            {
                var localDll = _localManifest.item.Find(e => e.name == remoteDll.name);
                if (localDll != null)
                {
                    if (remoteDll.hash != localDll.hash)
                    {
                        needDownloadDlls.Add(remoteDll);
                    }
                }
                else
                {
                    needDownloadDlls.Add(remoteDll);
                }
            }
            Debug.Log($"need download dlls count: {needDownloadDlls.Count}");

            foreach (var info in needDownloadDlls)
            {
                Debug.Log($"----need download dll: {info.name}");
            }
            return needDownloadDlls;
        }

        /// <summary>
        /// 下载dll
        /// </summary>
        /// <returns></returns>
        IEnumerator DownloadHotFixDlls()
        {
            var needDownloadDlls = CollectNeedDownloadDlls();
            if (needDownloadDlls.Count == 0)
                yield break;

            CheckNeedRestart(needDownloadDlls);

            Debug.Log("download dlls start....");
            var dir = Path.Combine(Application.persistentDataPath, "HotFixDll");
            if (Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            _totalSize = 0;

            //请求头部信息，获取下载大小
            foreach (var assembly in needDownloadDlls)
            {
                var url = Path.Combine(PathDefine.remoteHotFixDllPath, $"{assembly.name}.dll.bytes");

                UnityWebRequest headReq = UnityWebRequest.Head(url);
                yield return headReq.SendWebRequest();

                if (headReq.result == UnityWebRequest.Result.Success)
                {
                    string contentLength = headReq.GetResponseHeader("Content-Length");
                    if (!string.IsNullOrEmpty(contentLength))
                    {
                        _totalSize += ulong.Parse(contentLength);
                    }
                }
                else
                {
                    Debug.LogError($"request head failed: {url} - {headReq.error}");
                }
            }

            Debug.Log($"need download dll total size: {_totalSize}");

            // 下载文件
            ulong downloadedSize = 0;
            foreach (var assembly in needDownloadDlls)
            {
                var url = Path.Combine(PathDefine.remoteHotFixDllPath, $"{assembly.name}.dll.bytes");
                var savePath = Path.Combine(Application.persistentDataPath, "HotFixDll", $"{assembly.name}.dll.bytes");
                using (UnityWebRequest req = UnityWebRequest.Get(url))
                {
                    Debug.Log($"download dll {assembly.name}");
                    req.downloadHandler = new DownloadHandlerFile(savePath, append: false);
                    yield return req.SendWebRequest();

                    while (!req.isDone)
                    {
                        // 实时进度更新
                        string contentLength = req.GetResponseHeader("Content-Length");
                        ulong fileSize = !string.IsNullOrEmpty(contentLength) ? ulong.Parse(contentLength) : 0;
                        ulong currentDownloaded = (ulong)(req.downloadProgress * fileSize);

                        ulong totalDownloaded = downloadedSize + currentDownloaded;
                        float progress = _totalSize > 0 ? (float)totalDownloaded / _totalSize : 0f;

                        yield return null;
                    }

                    if (req.result != UnityWebRequest.Result.Success)
                    {
                        downloadedSize += req.downloadedBytes;
                        Debug.LogError($"download dll {assembly.name} failed, url: {url} error: {req.error}");
                    }
                    else
                    {
                        Debug.Log($"download dll {assembly.name} success. samePath: {savePath}");
                    }
                }
            }

            OverrideLocalManifest();
        }

        public void CheckNeedRestart(List<DllInfo> needDownloadList)
        {
            foreach (var dllInfo in _remoteManifest.item)
            {
                if (dllInfo.name == "GameEntry")
                {
                    foreach (var depName in dllInfo.dependencies)
                    {
                        if (needDownloadList.Find(x => x.name == depName) != null)
                        {
                            needRestart = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 覆盖本地manifest文件
        /// </summary>
        public void OverrideLocalManifest()
        {
            File.WriteAllText(PathDefine.persistentHotFixManifestPath, _remoteManifestJsonText);
        }

    }
}