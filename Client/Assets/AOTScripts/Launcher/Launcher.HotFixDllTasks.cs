using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Launcher
{
    public partial class Launcher : MonoBehaviour
    {
        HotFixDllManifest _localManifest;
        List<DllInfo> _needDownloadDlls;
        HotFixDllManifest _remoteManifest;
        string _remoteManifestJsonText;

        IEnumerator DoHotFixTasks()
        {
            yield return LoadLocalManifest();
            //下载manifest
            yield return DownloadManifest();

            if (_remoteManifest != null)
            {
                //收集要下载的dlls
                CollectNeedDownloadDlls();
                if (_needDownloadDlls.Count > 0)
                {
                    yield return DownloadHotFixDlls();
                }
            }

            yield return LoadHotFixDlls();

            if (_remoteManifest != null)
            {
                OverrideLocalManifest();
            }
        }

        IEnumerator LoadLocalManifest()
        {
            var localManifestText = string.Empty;

            var pathList = new List<string>() {Application.persistentDataPath,Application.streamingAssetsPath};

            foreach (var path in pathList)
            {
                var pathUrl = Path.Combine(path, "HotFixDllManifest.json");
                var uwr = UnityWebRequest.Get(pathUrl);
                yield return uwr.SendWebRequest();

                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    localManifestText = uwr.downloadHandler.text;
                    _localManifest = JsonUtility.FromJson<HotFixDllManifest>(localManifestText);
                    break;
                }
            }
        }

        IEnumerator DownloadManifest()
        {
            Debug.Log("下载 Manifest");
            var url = LauncherPathDefine.remoteHotFixDllManifest;
            var retryCount = 3;
            for (int i = 0; i < retryCount; i++)
            {
                using (UnityWebRequest uwr = UnityWebRequest.Get(url))
                {
                    // 设置超时时间（单位：秒）
                    uwr.timeout = 10;

                    yield return uwr.SendWebRequest();

                    if (uwr.result == UnityWebRequest.Result.Success)
                    {
                        Debug.Log("下载Manifest成功");
                        _remoteManifestJsonText = uwr.downloadHandler.text;
                        _remoteManifest = JsonUtility.FromJson<HotFixDllManifest>(_remoteManifestJsonText);
                        break;
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ 下载失败: {url}，错误: {uwr.error}");
                        // 如果是最后一次也失败了
                        if (i == retryCount - 1)
                        {
                            Debug.LogError($"❌ 最终下载失败: {url}");
                        }
                    }
                }

                yield return new WaitForSeconds(1f); // 可以加一个延迟再重试
            }
        }

        void CollectNeedDownloadDlls()
        {
            Debug.Log("收集要下载的dll");
            _needDownloadDlls = new List<DllInfo>();

            foreach (var remoteDll in _remoteManifest.item)
            {
                var localDll = _localManifest.item.Find(e => e.name == remoteDll.name);
                if (localDll != null)
                {
                    if (remoteDll.hash != localDll.hash)
                    {
                        _needDownloadDlls.Add(remoteDll);
                    }
                }
                else
                {
                    _needDownloadDlls.Add(remoteDll);
                }
            }

            Debug.Log($"需要下载dll数量: {_needDownloadDlls.Count}");
            foreach (var info in _needDownloadDlls)
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
            var allCnt = _needDownloadDlls.Count;
            foreach (var assembly in _needDownloadDlls)
            {
                var url = Path.Combine(LauncherPathDefine.remoteHotFixDllPath, $"{assembly.name}.dll.bytes");
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
            var manifest = _remoteManifest ?? _localManifest;
            var sortedList = GetLoadSortedList(manifest.item);
            Debug.Log($"加载所有HotFixDll count:{sortedList.Count}");
            foreach (var info in sortedList)
            {
                var dllPath = Path.Combine(LauncherPathDefine.persistentHotFixPath, $"{info.name}.dll.bytes");
                Debug.Log($"dllPath: {dllPath}");

                if (File.Exists(dllPath))
                {
                    var bytes = File.ReadAllBytes(dllPath);
                    Assembly.Load(bytes);
                    Debug.Log($"-----加载Dll成功{info.name}");
                    continue;
                }

                //StreamingAssets中的文件没办法用File.Exists判断
                dllPath = Path.Combine(LauncherPathDefine.originHotFixPath, $"{info.name}.dll.bytes");
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
        public static List<DllInfo> GetLoadSortedList(List<DllInfo> manifest)
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

            foreach (var asm in manifest)
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
            File.WriteAllText(LauncherPathDefine.persistentHotFixManifestPath, _remoteManifestJsonText);
        }
    }
}
