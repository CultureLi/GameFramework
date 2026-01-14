using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Assets.Editor.AssetPostProcessor
{
    internal class SpriteAtlasHandler : IAssetPostprocessorHandler
    {
        private const string UIRoot = "Assets/BundleRes/Arts/UI";

        public void OnPostprocessAllAssets(string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            HandleSpriteAtlasSetting(importedAssets);
        }

        void HandleSpriteAtlasSetting(string[] importedAssets)
        {
            foreach (var path in importedAssets)
            {
                if (!path.EndsWith(".spriteatlasv2"))
                    continue;

                // 只处理 UI 目录下的 Atlas
                if (!path.StartsWith(UIRoot))
                    continue;

                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
                if (atlas == null)
                    continue;

                ApplyUIAtlasSetting(atlas);

                // 标记脏，但不要 Import 自己
                EditorUtility.SetDirty(atlas);

                Debug.Log("Postprocess SpriteAtlas: " + path);
            }
        }

        private void ApplyUIAtlasSetting(SpriteAtlas atlas)
        {
            // ===== Pack Settings =====
            atlas.SetPackingSettings(new SpriteAtlasPackingSettings
            {
                enableRotation = false,
                enableTightPacking = false,
                padding = 4
            });

            // ===== Texture Settings =====
            atlas.SetTextureSettings(new SpriteAtlasTextureSettings
            {
                generateMipMaps = false,
                readable = false,
                sRGB = true,
                filterMode = FilterMode.Bilinear
            });

            // ===== 平台设置 =====
            SetPlatform(atlas, "Android", 2048, TextureImporterFormat.ASTC_6x6);
            SetPlatform(atlas, "iPhone", 2048, TextureImporterFormat.ASTC_6x6);
        }

        private void SetPlatform(
            SpriteAtlas atlas,
            string platform,
            int maxSize,
            TextureImporterFormat format)
        {
            var settings = atlas.GetPlatformSettings(platform);

            settings.overridden = true;
            settings.maxTextureSize = maxSize;
            settings.format = format;
            settings.compressionQuality = 50;

            atlas.SetPlatformSettings(settings);
        }
    }
}
