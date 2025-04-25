using Launcher.Runtime;
using Launcher.Runtime.Stage;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Assets.GameLauncher.Runtime.Stage
{
    [System.Serializable]
    public class DllInfo
    {
        public string name;
        public string hash;
        public List<string> dependencies;
    }
    [System.Serializable]
    public class HotFixDllManifest
    {
        public List<DllInfo> Item = new List<DllInfo>();
    }
    [System.Serializable]
    public class MetaDataInfo
    {
        public List<string> item = new List<string>();
    }

    internal class DownloadHotfixDll : StageBase
    {
        MonoBehaviour _runner;

        List<DllInfo> needDownloadDlls;
        HotFixDllManifest remoteManifest;

        public DownloadHotfixDll(MonoBehaviour runner)
        {
            _runner = runner;
        }
        protected internal override void OnEnter()
        {
            //加载元数据
            _runner.StartCoroutine(LoadMetaData());
        }

        IEnumerator LoadMetaData()
        {
            Debug.Log("补充元数据");
            var option = new ProviderLoadRequestOptions()
            {
                IgnoreFailures = true
            };
            var localPath = PathDefine.metaDataListPath;

            var req = UnityWebRequest.Get(localPath);
            yield return req.SendWebRequest();

            var text = req.downloadHandler.text;
            //Addressables.Release(handle);
            var metaDataInfo = JsonUtility.FromJson<MetaDataInfo>(text);

            foreach (var name in metaDataInfo.item)
            {
                localPath = Path.Combine(PathDefine.metaDataPath, name);

                var request = UnityWebRequest.Get(localPath);
                yield return request.SendWebRequest();

                int err = (int)HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(request.downloadHandler.data, HybridCLR.HomologousImageMode.SuperSet);
                Debug.Log($"补充元数据:{name}. ret:{err}");
            }

            OnLoadMetaDataCompleted();
        }

        void OnLoadMetaDataCompleted()
        {
            //下载最新hotfixManifest.json
            Debug.Log("下载Manifest Start");
            _runner.StartCoroutine(LauncherMgr.I.DownloadWithRetry(
                PathDefine.remoteHotFixDllManifest,
                null,
                3,
                10,
                OnDownloadManifestCompleted));
        }

        void OnDownloadManifestCompleted(DownloadHandler handler)
        {
            remoteManifest = JsonUtility.FromJson<HotFixDllManifest>(handler.text);
            Debug.Log($"下载Manifest End {remoteManifest.Item.Count}");

            var localPath = PathDefine.originHotFixDllManifest;
            var location = new ResourceLocationBase(localPath, localPath, typeof(TextDataProvider).FullName, typeof(string));
            location.Data = new ProviderLoadRequestOptions()
            {
                IgnoreFailures = true
            };

            var handle = Addressables.ResourceManager.ProvideResource<string>(location);
            handle.WaitForCompletion();

            var localManifest = JsonUtility.FromJson<HotFixDllManifest>(handle.Result);
            Addressables.Release(handle);

            needDownloadDlls = GetNeedDownloadDlls(localManifest.Item, remoteManifest.Item);
            Debug.Log($"需要下载dll数量: {needDownloadDlls.Count}");
            foreach (var info in needDownloadDlls)
            {
                Debug.Log($"----需要下载dll: {info.name}");
            }
            if (needDownloadDlls.Count > 0)
            {
                _runner.StartCoroutine(DownloadDlls());
            }
            else
            {
                _runner.StartCoroutine(LoadHotFixDlls());
            }
        }

        IEnumerator LoadHotFixDlls()
        {
            var sortedList = GetLoadSortedList(remoteManifest.Item);
            Debug.Log($"加载所有HotFixDll {sortedList.Count}");
            foreach (var info in sortedList)
            {
                var dllPath = Path.Combine(PathDefine.persistentHotFixPath, $"{info.name}.dll.bytes");
                Debug.Log($"dllPath: {dllPath}");

                if (File.Exists(dllPath))
                {
                    var bytes = File.ReadAllBytes(dllPath);
                    Assembly.Load(bytes);
                    Debug.Log($"-----加载Dll成功{info.name}");
                    continue;
                }

                //StreamingAssets中的文件没办法用File.Exists判断
                dllPath = Path.Combine(PathDefine.originHotFixPath, $"{info.name}.dll.bytes");
                Debug.Log($"dllPath: {dllPath}");
                var request = UnityWebRequest.Get(dllPath);
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"-----加载Dll成功{info.name}");
                    Assembly.Load(request.downloadHandler.data);
                }
            }

            ChangeStage<LauncherEndStage>();
        }

        IEnumerator DownloadDlls()
        {
            Debug.Log("下载Dlls");
            var dir = Path.Combine(Application.persistentDataPath, "HotFixDll");
            if (Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            var allCnt = needDownloadDlls.Count;
            foreach (var assembly in needDownloadDlls)
            {
                var url = Path.Combine(PathDefine.remoteHotFixDllPath, $"{assembly.name}.dll.bytes");
                var savePath = Path.Combine(Application.persistentDataPath, "HotFixDll", $"{assembly.name}.dll.bytes");
                using (UnityWebRequest www = UnityWebRequest.Get(url))
                {
                    www.downloadHandler = new DownloadHandlerFile(savePath, append: false);
                    yield return www.SendWebRequest();

                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"Download failed: {url}, Error: {www.error}");
                    }
                    else
                    {
                        allCnt--;
                        Debug.Log($"------下载完成: {savePath}");
                    }
                }
            }

            if (allCnt == 0)
            {
                _runner.StartCoroutine(LoadHotFixDlls());
            }
        }

        /// <summary>
        /// 收集变化的dll
        /// </summary>
        /// <param name="localManifest"></param>
        /// <param name="remoteManifest"></param>
        /// <returns></returns>
        public static List<DllInfo> GetNeedDownloadDlls(List<DllInfo> localManifest, List<DllInfo> remoteManifest)
        {
            var changedDlls = new List<DllInfo>();

            foreach (var remoteDll in remoteManifest)
            {
                bool isChanged = false;

                var localDll = localManifest.Find(e => e.name == remoteDll.name);
                if (localDll != null)
                {
                    if (remoteDll.hash != localDll.hash)
                    {
                        changedDlls.Add(remoteDll);
                    }
                }
                else
                {
                    changedDlls.Add(remoteDll);
                }
            }

            return changedDlls;
        }

        /// <summary>
        /// 按依赖顺序加载
        /// </summary>
        /// <param name="remoteManifest"></param>
        /// <returns></returns>
        public static List<DllInfo> GetLoadSortedList(List<DllInfo> remoteManifest)
        {
            List<DllInfo> sortedList = new List<DllInfo>();
            HashSet<string> visited = new HashSet<string>();

            void Visit(DllInfo info)
            {
                if (visited.Contains(info.name))
                    return;

                visited.Add(info.name);

                foreach (var dep in info.dependencies)
                {
                    Visit(info);
                }

                sortedList.Add(info);
            }

            foreach (var asm in remoteManifest)
            {
                Visit(asm);
            }

            return sortedList;
        }

    }
}
