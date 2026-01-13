using UnityEditor;

using System;
using System.Collections.Generic;

using Model = UnityEngine.AssetGraph.DataModel.Version2;
using UnityEditor.U2D;

namespace UnityEngine.AssetGraph
{
    [Serializable]
    [CustomAssetImporterConfigurator(typeof(SpriteAtlasImporter), "SpriteAtlas", "setting.spriteatlasv2")]
    public class SpriteAtlasImporterConfigurator : IAssetImporterConfigurator
    {
        [SerializeField] private bool m_overwritePackingTag;
        [SerializeField] private bool m_overwriteSpriteSheet;
        [SerializeField] private SerializableMultiTargetString m_customPackingTagTemplate;

        public void Initialize(ConfigurationOption option)
        {
            m_overwritePackingTag = option.overwritePackingTag;
            m_overwriteSpriteSheet = option.overwriteSpriteSheet;
            m_customPackingTagTemplate = option.customPackingTagTemplate;
        }

        public bool IsModified(AssetImporter referenceImporter, AssetImporter importer, BuildTarget target, string group)
        {
            var r = referenceImporter as SpriteAtlasImporter;
            var t = importer as SpriteAtlasImporter;
            if (t == null || r == null)
            {
                throw new AssetGraphException($"Invalid AssetImporter assigned for {importer.assetPath}");
            }
            return !IsEqual(t, r, GetTagName(target, group));
        }

        public void Configure(AssetImporter referenceImporter, AssetImporter importer, BuildTarget target, string group)
        {
            var r = referenceImporter as SpriteAtlasImporter;
            var t = importer as SpriteAtlasImporter;
            if (t == null || r == null)
            {
                throw new AssetGraphException($"Invalid AssetImporter assigned for {importer.assetPath}");
            }
            OverwriteImportSettings(t, r, GetTagName(target, group));
        }

        public void OnInspectorGUI(AssetImporter referenceImporter, BuildTargetGroup target, Action onValueChanged)
        {
            var importer = referenceImporter as SpriteAtlasImporter;
            if (importer == null)
            {
                return;
            }
        }

        private string GetTagName(BuildTarget target, string groupName)
        {
            return m_customPackingTagTemplate[target].Replace("*", groupName);
        }

        private void ApplySpriteTag(BuildTarget target, IEnumerable<PerformGraph.AssetGroups> incoming)
        {

            foreach (var ag in incoming)
            {
                foreach (var groupKey in ag.assetGroups.Keys)
                {
                    var assets = ag.assetGroups[groupKey];
                    foreach (var asset in assets)
                    {

                        if (asset.importerType == typeof(UnityEditor.TextureImporter))
                        {
                            var importer = AssetImporter.GetAtPath(asset.importFrom) as TextureImporter;

                            importer.spritePackingTag = GetTagName(target, groupKey);
                            importer.SaveAndReimport();
                            asset.TouchImportAsset();
                        }
                    }
                }
            }
        }
        private bool IsEqual(SpriteAtlasImporter target, SpriteAtlasImporter reference, string tagName)
        {
            return true;
        }

        private void OverwriteImportSettings(SpriteAtlasImporter target, SpriteAtlasImporter reference, string tagName)
        {

        }

        bool CompareImporterPlatformSettings(SpriteAtlasImporter c1, SpriteAtlasImporter c2)
        {


            return true;
        }
    }
}
