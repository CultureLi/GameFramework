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
        /// <summary>
        /// 在Sprite文件夹上右键，生成图集, 可以同时选择多个Sprite文件夹
        /// 生成规则：1
        /// SpriteParent
        ///     -Sprite
        ///         -Folder1     -> 生成图集 SpriteAtlas/SpriteParent_Folder1.SpriteAtlas
        ///         -Folder2     -> 生成图集 SpriteAtlas/SpriteParent_Folder2.SpriteAtlas

        /// 生成规则：2
        /// SpriteParent
        ///     -Sprite         -> 生成图集 SpriteAtlas/SpriteParent_Sprite.SpriteAtlas
        ///         -xxx.png
        ///         -Folder1
        /// </summary>
        [MenuItem("Assets/UI/生成图集", false, 1000)]
        static void GenerateAtlas()
        {
            foreach (var obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
            {
                string spriteRoot = AssetDatabase.GetAssetPath(obj);

                if (!AssetDatabase.IsValidFolder(spriteRoot) || !string.Equals(Path.GetFileName(spriteRoot), "Sprite"))
                {
                    Debug.LogError("需要选择Sprite文件夹！");
                    continue;
                }

                GenerateAtlas(spriteRoot);
            }
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/UI/生成所有图集", false, 1000)]
        static void GenerateAtlasAll()
        {
            var guids = AssetDatabase.FindAssets("t:Folder Sprite", new[] { "Assets/BundleRes/Arts/UI" });

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileName(path) == "Sprite")
                {
                    GenerateAtlas(path);
                }
            }
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 打包时候调用，打包前生成所有图集并打包
        /// </summary>
        public static void PackAllAtlases()
        {
            GenerateAtlasAll();
            SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);
        }   

        static bool GenerateAtlas(string spriteRoot)
        {
            bool hasChanged = false;
            var parentFolderName = Path.GetFileName(Path.GetDirectoryName(spriteRoot));
            string atlasFolder = Path.Combine(Path.GetDirectoryName(spriteRoot), "SpriteAtlas");
            if (!Directory.Exists(atlasFolder))
            {
                Directory.CreateDirectory(atlasFolder);
                AssetDatabase.Refresh();
            }

            // 获取Sprite目录下所有子文件夹
            var subFolders = Directory.GetDirectories(spriteRoot, "*", SearchOption.TopDirectoryOnly).ToList();

            // 根目录下有散图，就把根目录作为打包目录
            var hasLooseSprites = Directory.GetFiles(spriteRoot, "*.png", SearchOption.TopDirectoryOnly).Length > 0;
            if (hasLooseSprites)
            {
                subFolders.Clear();
                subFolders.Add(spriteRoot);
            }

            List<SpriteAtlas> atlasAssetPathList = new List<SpriteAtlas>();
            var tempSpriteAtlas = new SpriteAtlas();
            List<string> atlasNameList = new List<string>();
            foreach (var folder in subFolders)
            {
                string folderName = Path.GetFileName(folder);
                var atlasName = $"{parentFolderName}_{folderName}.spriteatlasv2";
                atlasNameList.Add(atlasName);
                string atlasPath = Path.Combine(atlasFolder, atlasName);
                string relativeAtlasPath = atlasPath.Replace(Application.dataPath, "Assets");

                string relativeFolder = folder.Replace(Application.dataPath, "Assets");
                Object folderObj = AssetDatabase.LoadAssetAtPath<Object>(relativeFolder);

                var atlasAsset = SpriteAtlasAsset.Load(atlasPath);
                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
                if (atlas == null)
                {
                    atlasAsset = new SpriteAtlasAsset();
                    atlas = new SpriteAtlas();
                    atlasAsset.SetMasterAtlas(atlas);
                    SpriteAtlasAsset.Save(atlasAsset, atlasPath);

                    hasChanged = true;
                    Debug.Log($"新建图集：{atlasPath}");
                }


                var packables = atlas.GetPackables();
                if (packables.Length != 1 || !packables.Contains(folderObj))
                {
                    hasChanged = true;
                    var objects = new Object[] { folderObj };
                    atlasAsset.Remove(atlas.GetPackables());
                    atlasAsset.Add(objects);
                    SpriteAtlasAsset.Save(atlasAsset, atlasPath);
                }

                var setting = atlas.GetPackingSettings();
                setting.enableRotation = false;
                setting.enableTightPacking = false;
                atlas.SetPackingSettings(setting);

                AssetDatabase.SaveAssets();
            }

            // 删除多余的图集
            var existingAtlases = Directory.GetFiles(atlasFolder, "*.spriteatlasv2", SearchOption.TopDirectoryOnly);
            foreach (var existingAtlasPath in existingAtlases)
            {
                string existingAtlasName = Path.GetFileName(existingAtlasPath);
                if (!atlasNameList.Contains(existingAtlasName))
                {
                    AssetDatabase.DeleteAsset(existingAtlasPath.Replace(Application.dataPath, "Assets"));
                    Debug.Log($"删除多余图集：{existingAtlasName}");
                }
            }
            return hasChanged;
        }
    }
}
