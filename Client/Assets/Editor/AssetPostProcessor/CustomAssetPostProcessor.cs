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
        void OnPostprocessAllAssets(CustomAssetPostProcessor.AssetTypePostData data);
    }

    internal class CustomAssetPostProcessor : AssetPostprocessor
    {
        public class AssetTypePostData
        {
            public List<string> importedAssets = new List<string>();
            public List<string> deletedAssets = new List<string>();
            public List<string> movedAssets = new List<string>();
            public List<string> movedFromAssetPaths = new List<string>();
        }

        enum EAssetChangeEvent
        {
            Imported,
            Deleted,
            Moved,
            MovedFrom,
        }

        static Dictionary<Type, AssetTypePostData> _postDataMap = new Dictionary<Type, AssetTypePostData>();
        static Dictionary<Type, IAssetPostprocessorHandler> _handleMap = new Dictionary<Type, IAssetPostprocessorHandler>()
        {
            { typeof(Texture), new TexturePostProcessorHandler() },
            { typeof(SpriteAtlas), new SpriteAtlasPostprocessorHandler() },
        };

        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            _postDataMap.Clear();
            FilterData(importedAssets, EAssetChangeEvent.Imported);
            FilterData(deletedAssets, EAssetChangeEvent.Deleted);
            FilterData(movedAssets, EAssetChangeEvent.Moved);
            FilterData(movedFromAssetPaths, EAssetChangeEvent.MovedFrom);

            foreach ((var type, var handler) in _handleMap)
            {
                handler.OnPostprocessAllAssets(_postDataMap.GetValueOrDefault(type));
            }
            _postDataMap.Clear();
        }

        static void AddData(Type type, string path, EAssetChangeEvent e)
        {
            if (!_handleMap.ContainsKey(type))
                return;

            if (!_postDataMap.TryGetValue(type, out var data))
            {
                data = new AssetTypePostData();
                _postDataMap[type] = data;
            }

            switch (e)
            {
                case EAssetChangeEvent.Imported:
                    data.importedAssets.Add(path);
                    break;
                case EAssetChangeEvent.Deleted:
                    data.deletedAssets.Add(path);
                    break;
                case EAssetChangeEvent.Moved:
                    data.movedAssets.Add(path);
                    break;
                case EAssetChangeEvent.MovedFrom:
                    data.movedFromAssetPaths.Add(path);
                    break;
            }
        }

        static void FilterData(string[] assets, EAssetChangeEvent e)
        {
            foreach (var path in assets)
            {
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (asset == null)
                {
                    foreach ((var t, var _) in _handleMap)
                    {
                        AddData(t, path, e);
                    }
                    continue;
                }
                var type = asset.GetType();
                AddData(type, path, e);
            }
        }
    }
}
