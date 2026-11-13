using AOTBase;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace Launcher
{
    public partial class Launcher : MonoBehaviour
    {
        HotFixDllManifest _localManifest;
        private void Awake()
        {
            Addressables.InitializeAsync().WaitForCompletion();
        }

        private void Start()
        {
            StartCoroutine(DoLaunch());
        }

        private IEnumerator DoLaunch()
        {
            yield return LoadMetaData();
            yield return LoadLocalManifest();
            yield return LoadHotFixDlls();

            EnterEntrance();
        }

        IEnumerator LoadMetaData()
        {
            Debug.Log("load metaDataList");
            var localPath = Path.Combine(Application.streamingAssetsPath, "metaDataList.json");
            var req = UnityWebRequest.Get(localPath);
            yield return req.SendWebRequest();

            var text = req.downloadHandler.text;
            var metaDataInfo = JsonUtility.FromJson<MetaDataInfo>(text);

            Debug.Log("load meta data start....");
            foreach (var name in metaDataInfo.item)
            {
                localPath = Path.Combine(Application.streamingAssetsPath, "MetaData", name);

                var request = UnityWebRequest.Get(localPath);
                yield return request.SendWebRequest();

                int err = (int)HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(request.downloadHandler.data, HybridCLR.HomologousImageMode.SuperSet);
            }
            Debug.Log("load meta data end....");
        }

        IEnumerator LoadLocalManifest()
        {
            var localManifestText = string.Empty;

            var pathList = new List<string>() { Application.persistentDataPath, Application.streamingAssetsPath };

            foreach (var path in pathList)
            {
                var pathUrl = Path.Combine(path, "hotFixDllManifest.json");
                var uwr = UnityWebRequest.Get(pathUrl);
                yield return uwr.SendWebRequest();

                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    localManifestText = uwr.downloadHandler.text;
                    _localManifest = JsonUtility.FromJson<HotFixDllManifest>(localManifestText);
                    break;
                }
            }

            if (_localManifest == null)
            {
                Debug.LogError("localManifect is Null");
            }
        }

        static bool IsAssemblyLoaded(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == name)
                    return true;
            }

            return false;
        }

        IEnumerator LoadHotFixDlls()
        {
            var sortedList = GetSortedDllList();
            Debug.Log($"HotFixDll count:{sortedList.Count}");
            foreach (var info in sortedList)
            {
                if (IsAssemblyLoaded(info.name))
                {
                    Debug.Log($"[Launcher] �����Ѽ��أ������ȸ� DLL: {info.name}");
                    continue;
                }

                var dllPath = Path.Combine(Application.persistentDataPath, "HotFixDll", $"{info.name}.dll.bytes");
                Debug.Log($"presistent dllPath: {dllPath}");

                if (File.Exists(dllPath))
                {
                    var bytes = File.ReadAllBytes(dllPath);
                    Assembly.Load(bytes);
                    Debug.Log($"presistent-----load dll: {info.name}");
                    continue;
                }

                dllPath = Path.Combine(Application.streamingAssetsPath, "HotFixDll", $"{info.name}.dll.bytes");
                Debug.Log($"streamingAsset dllPath: {dllPath}");
                var request = UnityWebRequest.Get(dllPath);
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Assembly.Load(request.downloadHandler.data);
                    Debug.Log($"streamingAsset-----load dll: {info.name}");
                }
            }
        }

        /// <summary>
        /// 把dll排个序，确保依赖的dll先加载
        /// </summary>
        /// <param name="remoteManifest"></param>
        /// <returns></returns>
        public List<DllInfo> GetSortedDllList()
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

            foreach (var asm in _localManifest.item)
            {
                Visit(asm);
            }

            return sortedList;
        }


        void EnterEntrance()
        {
            Debug.Log("EnterEntrance");
            var entranceAssembly = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "GameEntry");

            if (entranceAssembly == null)
            {
                Debug.LogError("entranceAssembly is Null");
                return;
            }

            Type entry = entranceAssembly.GetType("GameEntry.GameEntryMgr");
            if (entry == null)
            {
                Debug.LogError("GameEntryMgr is Null");
                return;
            }
            var method = entry.GetMethod("Entry");
            if (method == null)
            {
                Debug.LogError("Entry Method is Null");
                return;
            }
            method.Invoke(null, null);
        }

        private void OnDestroy()
        {

        }
    }

}
