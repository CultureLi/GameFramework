using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace GameLauncher.Runtime
{
    internal partial class PathDefine
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
        public static readonly string newestCalalogPath = Path.Combine(Application.persistentDataPath, "com.unity.addressables/intermediate/catalog.json");
        public static readonly string newestCalalogHashPath = newestCalalogPath.Replace(".json", ".hash");
        public static readonly string tempCalalogHashPath = newestCalalogPath.Replace("catalog.json", "catalogTemp.hash");
    }
}
