using AOTBase;
using Framework;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GameEntry.Stage
{
    /// <summary>
    /// 下载配置表，因为配置表很独立，没有相互依赖，且没有依赖任何unity资源
    /// 单独拿出来更新，不用构建bundle，更新便捷
    /// </summary>
    internal class DownloadConfigDataStage : FsmState
    {
        MonoBehaviour _runner;
        public DownloadConfigDataStage(MonoBehaviour runner)
        {
            _runner = runner;
        }

        protected override void OnEnter()
        {
            if (!GameConfig.I.checkHotUpdate)
            {
                ChangeState<EntranceEndStage>();
                return;
            }
            _runner.StartCoroutine(DoTask());
        }

        IEnumerator DoTask()
        {
            var localHash = FileUtility.ReadAllText(Path.Combine(PathDefine.persistentConfigDataPath, "configHash.hash"));
            if (string.IsNullOrEmpty(localHash))
            {
                localHash = FileUtility.ReadAllText(Path.Combine(PathDefine.originConfigDataPath, "configHash.hash"));
            }

            Debug.Log($"localConfigHash: {localHash}");
            
            var localHashMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(localHash);

            var remoteHash = string.Empty;
            yield return FW.ResourceMgr.DownloadRemoteFile(Path.Combine(PathDefine.remoteConfigDataPath,"configHash.hash"), handler =>
            {
                if (handler != null)
                {
                    remoteHash = handler.text;
                }
            });

            Debug.Log($"remoteConfigHash: {remoteHash}");

            var remoteHashMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(remoteHash);
            var needDownloadZip = new List<string>();

            foreach ((var key, var hash) in remoteHashMap)
            {
                if (localHashMap.TryGetValue(key, out var hash2))
                {
                    if (!string.Equals(hash, hash2))
                    {
                        needDownloadZip.Add(key);
                        Debug.Log($"need download zip {key}");
                    }
                }
                else
                {
                    needDownloadZip.Add(key);
                }
            }

            if (needDownloadZip.Count > 0)
            {
                var remoteHashUrl = Path.Combine(PathDefine.remoteConfigDataPath, "configHash.hash");
                if (!Directory.Exists(PathDefine.persistentConfigDataPath))
                {
                    Directory.CreateDirectory(PathDefine.persistentConfigDataPath);
                }

                var hashFileTarDir = Path.Combine(PathDefine.persistentConfigDataPath, "configHash.hash");

                foreach (var name in needDownloadZip)
                {
                    var remoteFile = Path.Combine(PathDefine.remoteConfigDataPath, name);
                    var tarFile = Path.Combine(PathDefine.persistentConfigDataPath, name);

                    yield return FW.ResourceMgr.DownloadRemoteFile(remoteFile, (loader) =>
                    {
                        using (MemoryStream zipStream = new MemoryStream(loader.data))
                        using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
                        {
                            foreach (var entry in archive.Entries)
                            {
                                string filePath = Path.Combine(PathDefine.persistentConfigDataPath, entry.FullName);
                                using (var entryStream = entry.Open())
                                using (var fileStream = File.Create(filePath))
                                {
                                    entryStream.CopyTo(fileStream);
                                }
                            }
                        }
                    });
                }

                File.WriteAllText(hashFileTarDir, remoteHash);
                FW.CfgMgr.Initialize();
            }

            ChangeState<EntranceEndStage>();
        }
    }
}
