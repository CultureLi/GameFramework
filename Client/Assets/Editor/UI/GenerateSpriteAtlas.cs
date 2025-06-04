using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Content;

namespace Assets.Editor
{
    public class GenerateSpriteAtlas
    {
        [MenuItem("Assets/生成图集", false, 1000)]
        static void GenerateAtlas()
        {
            foreach (var obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
            {
                string selectPath = AssetDatabase.GetAssetPath(obj);

                if (!AssetDatabase.IsValidFolder(selectPath))
                {
                    Debug.LogError("请选择一个文件夹！");
                    return;
                }

                string atlasFolder = Path.Combine(Path.GetDirectoryName(selectPath), "SpriteAtlas");
                if (!Directory.Exists(atlasFolder))
                {
                    Directory.CreateDirectory(atlasFolder);
                    AssetDatabase.Refresh();
                }

                string[] subFolders = Directory.GetDirectories(selectPath);
                List<SpriteAtlas> atlasAssetPathList = new List<SpriteAtlas>();
                foreach (var folder in subFolders)
                {
                    string folderName = Path.GetFileName(folder);
                    string atlasPath = Path.Combine(atlasFolder, folderName + ".spriteatlasv2");
                    string relativeAtlasPath = atlasPath.Replace(Application.dataPath, "Assets");
          
                    string relativeFolder = folder.Replace(Application.dataPath, "Assets");
                    Object folderObj = AssetDatabase.LoadAssetAtPath<Object>(relativeFolder);

                    SpriteAtlasAsset atlasAsset = null;

                    if (File.Exists(atlasPath))
                    {
                        // 已存在，加载图集
                        atlasAsset = SpriteAtlasAsset.Load(relativeAtlasPath);
                        if (atlasAsset == null)
                        {
                            Debug.LogError($"图集存在但无法加载：{relativeAtlasPath}");
                            continue;
                        }
                    }
                    else
                    {
                        // 新建图集
                        atlasAsset = new SpriteAtlasAsset();
                        Debug.Log($"新建图集：{relativeAtlasPath}");
                    }

                    atlasAssetPathList.Add(atlasAsset.GetMasterAtlas());
                    var objects = new Object[] { folderObj };
                    atlasAsset.Remove(objects);
                    atlasAsset.Add(objects);
                    SpriteAtlasAsset.Save(atlasAsset, relativeAtlasPath);
                    AssetDatabase.Refresh();

                    var sai = (SpriteAtlasImporter)AssetImporter.GetAtPath(relativeAtlasPath);
                    var ps = sai.packingSettings;
                    ps.enableTightPacking = false;
                    ps.enableRotation = false;
                    sai.packingSettings = ps;
                    AssetDatabase.WriteImportSettingsIfDirty(relativeAtlasPath);
                }
            }
        }
    }
}
