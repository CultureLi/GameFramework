using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using Framework;
using Assets.Editor.Build;

namespace Assets.Editor
{
    public static class GenerateSpriteMapper
    {
        [MenuItem("Tools/UI/生成SpriteMapper", false, 1000)]
        public static void GenSpriteMapper()
        {
            Debug.Log("GenSpriteMapper Stat..............");
            var st = System.Diagnostics.Stopwatch.StartNew();
            var so = AssetDatabase.LoadAssetAtPath<SpriteMapper>("Assets/BundleRes/ScriptableObject/SpriteMapper.asset");

            var setting = AddressableAssetSettingsDefaultObject.Settings;
            so.Clear();
            CollectAtlasSprites(setting, so);
            CollectSingleSprites(setting, so);
            //CollectSprites(setting, so);

            EditorUtility.SetDirty(so);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            BuildTools.LogTime("GenSpriteMapper", st.ElapsedMilliseconds);
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

                string atlasPath = AssetDatabase.GUIDToAssetPath(guid);
                SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);

                var sprites = new Sprite[atlas.spriteCount];
                atlas.GetSprites(sprites);

                foreach (var sprite in sprites)
                {
                    var spriteName = sprite.name;
                    if (spriteName.EndsWith("(Clone)", StringComparison.Ordinal))
                        spriteName = spriteName.Replace("(Clone)", "");
                    var address = $"{assetEntry.address}[{spriteName}]";
                    mapper.Add(spriteName, address);
                }
            }
        }

        public static void ModifyAtlasMap(AddressableAssetSettings setting, SpriteMapper mapper, string atlasPath, bool add)
        {
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            var guid = AssetDatabase.AssetPathToGUID(atlasPath);
            var assetEntry = setting.FindAssetEntry(guid, true);

            var sprites = new Sprite[atlas.spriteCount];
            atlas.GetSprites(sprites);

            foreach (var sprite in sprites)
            {
                var spriteName = sprite.name;
                if (spriteName.EndsWith("(Clone)", StringComparison.Ordinal))
                    spriteName = spriteName.Replace("(Clone)", "");
                var address = $"{assetEntry.address}[{spriteName}]";

                if (add)
                {
                    mapper.Add(spriteName, address);
                }
                else
                {
                    mapper.Remove(spriteName);
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

                    var spriteName = sprite.name;
                    if (spriteName.EndsWith("(Clone)", StringComparison.Ordinal))
                        spriteName = spriteName.Replace("(Clone)", "");

                    mapper.Add(spriteName, spriteEntry.address);
                }
            }
        }

        //[MenuItem("Tools/UI/Clear SpriteMapper", false, 1000)]
        public static void ClearSpriteMapper()
        {
            var so = AssetDatabase.LoadAssetAtPath<SpriteMapper>("Assets/BundleRes/ScriptableObject/SpriteMapper.asset");

            var setting = AddressableAssetSettingsDefaultObject.Settings;

            so.Clear();
            EditorUtility.SetDirty(so);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}