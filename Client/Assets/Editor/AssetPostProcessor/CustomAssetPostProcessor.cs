using Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

namespace Assets.Editor.AssetPostProcessor
{
    internal interface IAssetPostprocessorHandler
    {
        void OnPostprocessAllAssets(string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths);
    }

    internal class CustomAssetPostProcessor : AssetPostprocessor
    {
        static List<IAssetPostprocessorHandler> _handles = new List<IAssetPostprocessorHandler>()
        {
            new TextureHandler(),
            new SpriteAtlasHandler(),
            new SpriteMapperHandler(),
        };

        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (var handler in _handles)
            {
                handler.OnPostprocessAllAssets(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
            }
        }
    }
}
