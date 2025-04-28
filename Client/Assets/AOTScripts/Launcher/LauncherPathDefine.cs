using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Launcher
{
    public partial class LauncherPathDefine
    {
        private static string platformSubFolder = PlatformMappingService.GetPlatformPathSubFolder();

        private static readonly string remoteUrl = "http://10.23.50.187:7888/";

        public static readonly string remoteHotFixDllPath = Path.Combine(remoteUrl, platformSubFolder, "HotFixDll");

        public static readonly string remoteHotFixDllManifest = Path.Combine(remoteUrl, platformSubFolder, "hotFixDllManifest.json");

        public static readonly string originHotFixDllManifest = Path.Combine(Application.streamingAssetsPath, "hotFixDllManifest.json");

        public static readonly string persistentHotFixManifestPath = Path.Combine(Application.persistentDataPath, "hotFixDllManifest.json");

        public static readonly string metaDataPath = Path.Combine(Application.streamingAssetsPath, "MetaData");
        public static readonly string metaDataListPath = Path.Combine(Application.streamingAssetsPath, "metaDataList.json");

        public static readonly string originHotFixPath = Path.Combine(Application.streamingAssetsPath, "HotFixDll");

        public static readonly string persistentHotFixPath = Path.Combine(Application.persistentDataPath, "HotFixDll");

    }
}
