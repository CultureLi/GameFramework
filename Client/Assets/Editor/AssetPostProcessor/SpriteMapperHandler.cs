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
    internal class SpriteMapperHandler : IAssetPostprocessorHandler
    {
        public void OnPostprocessAllAssets(string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (importedAssets.Concat(deletedAssets).Concat(movedAssets).Concat(movedFromAssetPaths)
                .Any(path => path.StartsWith("Assets/BundleRes/Arts/UI")))
            {
                GenerateSpriteMapper.GenSpriteMapper();
            }
        }
    }
}
