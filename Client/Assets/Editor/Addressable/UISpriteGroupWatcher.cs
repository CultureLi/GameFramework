/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor;
using Framework;

namespace Assets.Editor.Addressable
{
    [InitializeOnLoad]
    public static class UISpriteGroupWatcher
    {
        private const string groupName = "UISprite";

        private static readonly List<(AddressableAssetEntry, AddressableAssetSettings.ModificationEvent)> ChangedEntries
            = new List<(AddressableAssetEntry, AddressableAssetSettings.ModificationEvent)>();

        static UISpriteGroupWatcher()
        {
            AddressableAssetSettings.OnModificationGlobal += OnAddressablesModified;
        }

        private static void OnAddressablesModified(AddressableAssetSettings settings, AddressableAssetSettings.ModificationEvent evt, object data)
        {
            switch (evt)
            {
                case AddressableAssetSettings.ModificationEvent.EntryAdded:
                case AddressableAssetSettings.ModificationEvent.EntryRemoved:
                case AddressableAssetSettings.ModificationEvent.EntryMoved:
                case AddressableAssetSettings.ModificationEvent.EntryModified:
                    Collect(data, evt);
                    break;
            }

            if (ChangedEntries.Count > 0)
            {
                EditorApplication.delayCall -= ProcessChanges;
                EditorApplication.delayCall += ProcessChanges;
            }
        }
        private static void Collect(object data, AddressableAssetSettings.ModificationEvent evt)
        {
            if (data is AddressableAssetEntry entry)
            {
                if (IsInUIGroup(entry))
                    ChangedEntries.Add((entry, evt));
            }
            else if (data is IEnumerable<AddressableAssetEntry> entries)
            {
                foreach (var e in entries)
                {
                    if (IsInUIGroup(e))
                        ChangedEntries.Add((e, evt));
                }
            }
        }

        private static bool IsInUIGroup(AddressableAssetEntry entry)
        {
            return entry.parentGroup != null
                && entry.parentGroup.Name == groupName;
        }

        private static void ProcessChanges()
        {
            try
            {
                var mapper = AssetDatabase.LoadAssetAtPath<SpriteMapper>("Assets/BundleRes/ScriptableObject/SpriteMapper.asset");
                foreach (var entry in ChangedEntries)
                {
                    OnUIGroupEntryChanged(mapper, entry.Item1, entry.Item2);
                }
            }
            finally
            {
                ChangedEntries.Clear();
            }
        }

        private static void OnUIGroupEntryChanged(SpriteMapper mapper,
            AddressableAssetEntry entry, AddressableAssetSettings.ModificationEvent evt)
        {
            // 你要的回调逻辑
            // 比如：更新 SpriteMapper

            string guid = entry.guid;
            string address = entry.address;
            string assetPath = entry.AssetPath;

            *//*var spriteName
            if (evt == AddressableAssetSettings.ModificationEvent.EntryRemoved)
            {
                mapper.Remove()
            }*//*
            // 示例
            UnityEngine.Debug.Log(
                $"[UIGroup Changed] {evt} | {entry.AssetPath} {entry.address}");
        }
    }
}
*/