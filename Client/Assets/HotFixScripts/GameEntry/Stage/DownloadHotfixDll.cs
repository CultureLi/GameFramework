/*
using Entrance.Stage;
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
    // 所以打初始包时，需要将这些dll放在StreamingAssets中
    // 通过远端manifest和本地manifest对比，下载最新dll
    // 根据manifest中依赖关系，按顺序依次加载dll, 查找dll顺序为先PersistentPath,后StreamingAssets


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

    internal class DownloadHotfixDll : StageBase
    {
        MonoBehaviour _runner;

        List<DllInfo> needDownloadDlls;
        HotFixDllManifest remoteManifest;
        string remoteManifestJsonText;

        public DownloadHotfixDll(MonoBehaviour runner)
        {
            _runner = runner;
        }
        protected internal override void OnEnter()
        {
            _runner.StartCoroutine(Start());
        }

        IEnumerator Start()
        {
            //下载manifest
            yield return DownloadManifest();
            //收集要下载的dlls
            yield return CollectNeedDownloadDlls();
            if (needDownloadDlls.Count > 0)
            {
                yield return DownloadHotFixDlls();
            }

#if !UNITY_EDITOR
            //加载热更dlls
            yield return LoadHotFixDlls();
#endif
            OverrideLocalManifest();

            ChangeStage<EntranceEndStage>();
        }

       
        IEnumerator DownloadManifest()
        {
            Debug.Log("下载 Manifest");
            yield return GameEntryMgr.I.DownloadWithRetry(
                PathDefine.remoteHotFixDllManifest,
                null,
                3,
                10,
                (handler) =>
                {
                    Debug.Log("下载 Manifest Success!!!!");
                    remoteManifestJsonText = handler.text;
                    remoteManifest = JsonUtility.FromJson<HotFixDllManifest>(remoteManifestJsonText);
                }
                );
        }

        IEnumerator CollectNeedDownloadDlls()
        {
            Debug.Log("收集要下载的dll");
            var localManifestText = string.Empty;
            if (File.Exists(PathDefine.persistentHotFixManifestPath))
            {
                localManifestText = File.ReadAllText(PathDefine.persistentHotFixManifestPath);
            }
            else
            {
                var request = UnityWebRequest.Get(PathDefine.originHotFixDllManifest);
                yield return request.SendWebRequest();

                localManifestText = request.downloadHandler.text;
            }

            var localManifest = JsonUtility.FromJson<HotFixDllManifest>(localManifestText);

            needDownloadDlls = GetNeedDownloadDlls(localManifest.Item, remoteManifest.Item);
            Debug.Log($"需要下载dll数量: {needDownloadDlls.Count}");
            foreach (var info in needDownloadDlls)
            {
                Debug.Log($"----需要下载dll: {info.name}");
            }
           
        }

        IEnumerator DownloadHotFixDlls()
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
        }

        IEnumerator LoadHotFixDlls()
        {
            var sortedList = GetLoadSortedList(remoteManifest.Item);
            Debug.Log($"加载所有HotFixDll count:{sortedList.Count}");
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

        /// <summary>
        /// 覆盖本地manifest文件
        /// </summary>
        public void OverrideLocalManifest()
        {
            File.WriteAllText(PathDefine.persistentHotFixManifestPath, remoteManifestJsonText);
        }

    }
}
*/