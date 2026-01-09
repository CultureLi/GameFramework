using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor.U2D;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using Framework;

namespace Assets.Editor
{
    public static class GenerateSpriteMapper
    {
        [MenuItem("Tools/UI/生成Sprite Mapper", false, 1000)]
        public static void GenSpriteMapper()
        {
            Debug.Log("GenSpriteMapper Stat...............");
            var so = AssetDatabase.LoadAssetAtPath<SpriteMapper>("Assets/BundleRes/ScriptableObject/SpriteMapper.asset");

            var setting = AddressableAssetSettingsDefaultObject.Settings;

            so.Clear();
            CollectAtlasSprites(setting, so);
            CollectSingleSprites(setting, so);

            EditorUtility.SetDirty(so);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("GenSpriteMapper End...............");
        }

        private static void CollectAtlasSprites(AddressableAssetSettings settings, SpriteMapper mapper)
        {
            var atlasGuids = AssetDatabase.FindAssets(
                "t:SpriteAtlas",
                new[] { "Assets/BundleRes/Arts/UI" }
            );

            foreach (var guid in atlasGuids)
            {
                var assetEntry = settings.FindAssetEntry(guid, true);

                if (assetEntry == null)
                    continue;

                string path = AssetDatabase.GUIDToAssetPath(guid);
                SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);

                var sprites = new Sprite[atlas.spriteCount];
                atlas.GetSprites(sprites);

                foreach (var sprite in sprites)
                {
                    if (sprite)
                    {
                        mapper.Add(sprite, assetEntry.address);
                    }
                }
            }
        }

        private static void CollectSingleSprites(AddressableAssetSettings settings, SpriteMapper mapper)
        {
            //找到所有名为 SingleSprite 的文件夹
            var folderGuids = AssetDatabase.FindAssets("SingleSprite", new[] { "Assets/BundleRes/Arts/UI" });

            foreach (var folderGuid in folderGuids)
            {
                var folderPath = AssetDatabase.GUIDToAssetPath(folderGuid);

                // 只处理文件夹
                if (!AssetDatabase.IsValidFolder(folderPath))
                    continue;

                // 在 SingleSprite 文件夹下找所有 Sprite
                var spriteGuids = AssetDatabase.FindAssets(
                    "t:Sprite",
                    new[] { folderPath }
                );

                foreach (var spriteGuid in spriteGuids)
                {
                    var spriteEntry = settings.FindAssetEntry(spriteGuid, true);
                    if (spriteEntry == null)
                        continue;

                    string spritePath = AssetDatabase.GUIDToAssetPath(spriteGuid);
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                    if (sprite == null)
                        continue;

                    mapper.Add(sprite, spriteEntry.address);
                }
            }
        }
    }
}