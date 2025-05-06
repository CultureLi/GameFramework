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
        IEnumerator LoadMetaData()
        {
            Debug.Log("加载metaDataList");
            var localPath = Path.Combine(Application.streamingAssetsPath, "metaDataList.json");
            var req = UnityWebRequest.Get(localPath);
            yield return req.SendWebRequest();

            var text = req.downloadHandler.text;
            var metaDataInfo = JsonUtility.FromJson<MetaDataInfo>(text);

            Debug.Log("补充元数据");
            foreach (var name in metaDataInfo.item)
            {
                localPath = Path.Combine(Application.streamingAssetsPath,"MetaData", name);

                var request = UnityWebRequest.Get(localPath);
                yield return request.SendWebRequest();

                int err = (int)HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(request.downloadHandler.data, HybridCLR.HomologousImageMode.SuperSet);
                Debug.Log($"补充元数据:{name}. ret:{err}");
            }
        }
    }

}
