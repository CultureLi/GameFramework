using GameEntry;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor.Build
{
    public partial class BuildTools
    {

        [MenuItem("BuildTools/ConfigData/CopyToServer")]
        public static void CopyConfigDataToServer()
        {
            Debug.Log("Config CopyConfigData Start ...");
            try
            {
                var remoteConfigPath = Path.Combine("../HttpServer", "Config");
                if (Directory.Exists(remoteConfigPath))
                {
                    Directory.Delete(remoteConfigPath, true);
                }

                Directory.CreateDirectory(remoteConfigPath);

                var files = Directory.GetFiles(PathDefine.originConfigDataPath, "*.*", SearchOption.AllDirectories)
                             .Where(f => !f.EndsWith(".meta"))
                             .ToArray();

                foreach (var file in files)
                {
                    var name = Path.GetFileName(file);
                    File.Copy(file, Path.Combine(remoteConfigPath, name));
                }
                Debug.Log("Config CopyConfigData Completed ...");
            }
            catch (Exception e)
            {
                Debug.LogError($"CopyConfigData {e}");
            }
        }
    }
    
}
