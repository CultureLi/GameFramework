using Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor.AssetPostProcessor
{
    internal class TextureHandler : IAssetPostprocessorHandler
    {
        public void OnPostprocessAllAssets(string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            HandleUISpriteSetting(importedAssets);
        }

        void HandleUISpriteSetting(string[] importedAssets)
        {
            foreach (var assetPath in importedAssets)
            {
                if (!assetPath.StartsWith("Assets/BundleRes/Arts/UI"))
                    continue;

                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;

                SetPlatform(importer, "Android", 2048, TextureImporterFormat.ASTC_6x6);
                SetPlatform(importer, "iPhone", 2048, TextureImporterFormat.ASTC_6x6);

                Debug.Log("Postprocess Sprite: " + assetPath);
            }
        }

        void SetPlatform(
            TextureImporter importer,
            string platform,
            int maxSize,
            TextureImporterFormat format)
        {
            var setting = importer.GetPlatformTextureSettings(platform);
            setting.overridden = true;
            setting.maxTextureSize = maxSize;
            setting.format = format;
            importer.SetPlatformTextureSettings(setting);
        }
    }
}
