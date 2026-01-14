using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor;
using UnityEngine.AddressableAssets;
using UnityEngine;
using System.IO;

namespace Assets.Editor.Build
{
    public partial class BuildTools
    {
		[MenuItem("BuildTools/Addressable/Clear Cache")]
		public static void ClearAddressablesCache()
		{
			bool success = Caching.ClearCache(); // 清空 Unity 的缓存系统（默认存储下载的 AssetBundles）
			var inter = Path.Combine(Application.persistentDataPath, "com.unity.addressables/intermediate");
			if (Directory.Exists(inter))
				Directory.Delete(inter, true);

			if (success)
				Debug.Log("Addressables 缓存清理成功！");
			else
				Debug.LogWarning("缓存清理失败，可能没有可清理的缓存。");
		}

        [MenuItem("BuildTools/Addressable/BuildAll")]
        public static void BuildAddressables()
		{
			Debug.Log("Addressables Build Start ...");
            var st = System.Diagnostics.Stopwatch.StartNew();
            AddressableAssetSettings.CleanPlayerContent();
			try
			{
				AddressableAssetSettings.BuildPlayerContent();
			}
			catch (Exception e)
			{
				Debug.LogError($"BuildPlayerContent {e}");
			}

			Debug.Log("Addressables Build Finished !!! ");

			AssetDatabase.Refresh();
            BuildTools.LogTime("BuildAddressables", st.ElapsedMilliseconds);
            CopyBundlesToServer();
			
		}

		[MenuItem("BuildTools/Addressable/CopyBundlesToServer")]
		public static void CopyBundlesToServer()
		{
			Debug.Log("Addressables CopyBundles Start ...");
			try
			{
                if (!Directory.Exists(buildParams.platformDir))
                {
                    Directory.CreateDirectory(buildParams.platformDir);
                }
                
                var bundleDir = Path.Combine(buildParams.platformDir, "Bundles");
                if (Directory.Exists(bundleDir))
                {
                    Directory.Delete(bundleDir, true);
                }
                Directory.CreateDirectory(bundleDir);

				var files = Directory.GetFiles(Addressables.BuildPath, "*.bundle", SearchOption.AllDirectories);
				foreach (var file in files)
				{
					var name = Path.GetFileName(file);
					File.Copy(file, Path.Combine(bundleDir, name), true);
				}

                files = Directory.GetFiles(Addressables.BuildPath, "catalog.*");
                foreach (var file in files)
                {
                    var name = Path.GetFileName(file);
                    File.Copy(file, Path.Combine(buildParams.platformDir, name), true);
                }

                //FileUtil.CopyFileOrDirectory(Addressables.BuildPath, remoteBuildPath);
                Debug.Log("Addressables CopyBundles Completed ...");
			}
			catch (Exception e)
			{
				Debug.LogError($"CopyBundlesToServerData {e}");
			}
		}

	}
}
