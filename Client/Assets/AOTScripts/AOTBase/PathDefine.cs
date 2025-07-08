using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AOTBase
{
    public partial class PathDefine
    {
        public static string platformSubFolder = PlatformMappingService.GetPlatformPathSubFolder();

        public static readonly string remoteUrl = "http://10.23.50.187:7888/";
        public static readonly string remoteBundleUrl = Path.Combine(remoteUrl, platformSubFolder, platformSubFolder);

        public static readonly string remoteCatalogUrl = Path.Combine(remoteUrl, platformSubFolder, "catalog.json");
        public static readonly string remoteCatalogHashUrl = remoteCatalogUrl.Replace(".json", ".hash");

        public static readonly string remoteVersionUrl = Path.Combine(remoteUrl, platformSubFolder, "version.txt");


        //随安装包一起的原始catalog路径
        public static readonly string originCatalogPath = Path.Combine(Addressables.RuntimePath, "catalog.json");
        public static readonly string originCatalogHashPath = originCatalogPath.Replace(".json", ".hash");

        public static readonly string persistentBundlePath = Path.Combine(Application.persistentDataPath, "com.unity.addressables/bundles");
        //经过热更后下载的最新Catalog
        public static readonly string persistentCatalogPath = Path.Combine(Application.persistentDataPath, "catalog.json");
        public static readonly string persistentCatalogHashPath = persistentCatalogPath.Replace(".json", ".hash");
        public static readonly string tempCalalogHashPath = persistentCatalogPath.Replace("catalog.json", "catalogTemp.hash");

        public static readonly string remoteHotFixDllPath = Path.Combine(remoteUrl, platformSubFolder, "HotFixDll");

        public static readonly string remoteHotFixDllManifest = Path.Combine(remoteUrl, platformSubFolder, "hotFixDllManifest.json");

        public static readonly string originHotFixDllManifest = Path.Combine(Application.streamingAssetsPath, "hotFixDllManifest.json");

        public static readonly string persistentHotFixManifestPath = Path.Combine(Application.persistentDataPath, "hotFixDllManifest.json");

        public static readonly string metaDataPath = Path.Combine(Application.streamingAssetsPath, "MetaData");
        public static readonly string metaDataListPath = Path.Combine(Application.streamingAssetsPath, "metaDataList.json");

        public static readonly string originHotFixPath = Path.Combine(Application.streamingAssetsPath, "HotFixDll");

        public static readonly string persistentHotFixPath = Path.Combine(Application.persistentDataPath, "HotFixDll");

        public static readonly string persistentConfigDataPath = Path.Combine(Application.persistentDataPath, "Config");

        public static readonly string remoteConfigDataPath = Path.Combine(remoteUrl, "Config");

        public static readonly string originConfigDataPath = Path.Combine(Application.streamingAssetsPath, "Config");
    }
}
