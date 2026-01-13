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
    internal class TexturePostProcessorHandler : IAssetPostprocessorHandler
    {
        public void OnPostprocessAllAssets(CustomAssetPostProcessor.AssetTypePostData data)
        {
            if (data is null)
                return;

            HandleUISpriteSetting(data.importedAssets);
            RefreshSpriteMapper(data);
        }

        void HandleUISpriteSetting(List<string> assetPaths)
        {
            foreach (var assetPath in assetPaths)
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

        /// <summary>
        /// 刷新 SpriteMapper，这里可以做的更细一些，只修改变动的部分
        /// </summary>
        /// <param name="data"></param>
        void RefreshSpriteMapper(CustomAssetPostProcessor.AssetTypePostData data)
        {
            var pathList = data.importedAssets.Concat(data.movedAssets).Concat(data.deletedAssets).Concat(data.movedFromAssetPaths);
            foreach (var assetPath in pathList)
            {
                if (assetPath.StartsWith("Assets/BundleRes/Arts/UI"))
                {
                    //GenerateSpriteMapper.GenSpriteMapper();
                    break;
                }
            }
        }
    }
}
