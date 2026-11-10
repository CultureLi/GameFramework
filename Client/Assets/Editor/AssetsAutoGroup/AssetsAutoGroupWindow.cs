using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace GameFramework.EditorTools.AssetsAutoGroup
{
    /// <summary>
    /// 资源自动分组工具（需求文档：需求文档/资源管理.md）。
    ///
    /// 根据 doc 中的目录结构，重建 Addressable Importer 规则：
    ///   - 先清空 Arts / AssetBundles 下已有的 Addressables 条目；
    ///   - 生成「文件夹 -> Group -> 打包模板」规则；
    ///   - 重新导入两个根目录，让 Addressable Importer 重新建立条目。
    ///
    /// 打包模板：
    ///   packType 1 = PackSeparately（每个文件单独 Bundle）
    ///   packType 2 = PackTogether（整个 Group 一个 Bundle）
    ///
    /// 说明：
    ///   - 文档中 UISprite 同时出现了 packType 1 和 2；Addressables 一个 Group
    ///     只能有一种打包模式，本工具按 packType 1（PackSeparately）处理。
    ///   - 文档文件夹列表把 SkyBox 写成 group:9，但分组定义里有独立的 SkyBox 组，
    ///     本工具使用 SkyBox 组。
    /// </summary>
    public sealed class AssetsAutoGroupWindow : EditorWindow
    {
        private const string MenuPath = "Tools/AssetsAutoGroup";
        private const string ImportSettingsPath = "Assets/AddressableAssetsData/AddressableImportSettings.asset";
        private const string PackSeparatelyPath = "Assets/AddressableAssetsData/AssetGroupTemplates/PackSeparately.asset";
        private const string PackTogetherPath = "Assets/AddressableAssetsData/AssetGroupTemplates/PackTogether.asset";

        private static readonly string[] ImportRoots =
        {
            "Assets/Arts",
            "Assets/AssetBundles",
        };

        /// <summary>需要先清空的资源根目录；BundleRes 是重组前的旧目录，用来清掉残留条目。</summary>
        private static readonly string[] ManagedAssetRoots =
        {
            "Assets/Arts/",
            "Assets/AssetBundles/",
        };

        private sealed class RuleSpec
        {
            public string Folder;
            public string Group;
            public int PackType;
            public string Extensions;
        }

        private bool _showRules;
        private string _lastResult;

        [MenuItem(MenuPath)]
        private static void Open()
        {
            var window = GetWindow<AssetsAutoGroupWindow>("Assets Auto Group");
            window.minSize = new Vector2(620f, 520f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("资源自动分组", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "会先清空 Arts / AssetBundles 下已有的 Addressables 条目，然后按资源管理文档重建\n" +
                "Addressable Importer 规则并重新导入两个根目录。\n\n" +
                "packType 1 = 每个文件单独 Bundle；packType 2 = 整个 Group 一个 Bundle。",
                MessageType.Info);

            var specs = BuildSpecs();
            var groupModes = BuildGroupModes(specs);

            EditorGUILayout.LabelField("规则数量", specs.Count.ToString());
            EditorGUILayout.LabelField("分组数量", groupModes.Count.ToString());

            var conflicts = FindPackTypeConflicts(specs);
            if (conflicts.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("以下 Group 在文档里有多种 packType，Addressables 只能选一种，本工具按 packType 1 处理：");
                foreach (var conflict in conflicts)
                    sb.AppendLine("  " + conflict);
                EditorGUILayout.HelpBox(sb.ToString(), MessageType.Warning);
            }

            EditorGUILayout.HelpBox(
                "文档文件夹列表把 SkyBox 写成 group:9，但分组定义里有独立 SkyBox 组；本工具使用 SkyBox 组。",
                MessageType.Warning);

            _showRules = EditorGUILayout.Foldout(_showRules, "规则明细", true);
            if (_showRules)
            {
                _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(260f));
                foreach (var spec in specs)
                {
                    string label = spec.Folder + "  ->  " + spec.Group +
                                   "  [" + PackTypeName(groupModes[spec.Group]) + "]";
                    if (!string.IsNullOrEmpty(spec.Extensions))
                        label += "  " + spec.Extensions;

                    EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
                }
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("执行自动分组", GUILayout.Height(36f)))
            {
                if (EditorUtility.DisplayDialog(
                        "资源自动分组",
                        "将清空 Arts / AssetBundles 下已有的 Addressables 条目，并按规则重新分组。\n\n" +
                        "确定继续？",
                        "开始", "取消"))
                {
                    Execute();
                }
            }

            if (!string.IsNullOrEmpty(_lastResult))
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(_lastResult, MessageType.None);
            }
        }

        private Vector2 _scroll;

        private static void Execute()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                EditorUtility.DisplayDialog("资源自动分组",
                    "找不到 Addressables 设置，请先在 Addressables 窗口创建设置。", "OK");
                return;
            }

            var importSettings = AssetDatabase.LoadAssetAtPath<AddressableImportSettings>(ImportSettingsPath);
            if (importSettings == null)
            {
                EditorUtility.DisplayDialog("资源自动分组",
                    "找不到 Addressable Importer 设置:\n" + ImportSettingsPath, "OK");
                return;
            }

            if (AddressableImportSettingsList.Instance == null)
            {
                EditorUtility.DisplayDialog("资源自动分组",
                    "找不到 AddressableImportSettingsList，Addressable Importer 不会执行规则。\n" +
                    "请先在 AddressableAssetsData 目录创建 Import Settings。", "OK");
                return;
            }

            var separateTemplate = AssetDatabase.LoadAssetAtPath<AddressableAssetGroupTemplate>(PackSeparatelyPath);
            var togetherTemplate = AssetDatabase.LoadAssetAtPath<AddressableAssetGroupTemplate>(PackTogetherPath);
            if (separateTemplate == null || togetherTemplate == null)
            {
                EditorUtility.DisplayDialog("资源自动分组",
                    "找不到 PackSeparately / PackTogether 分组模板。", "OK");
                return;
            }

            var specs = BuildSpecs();
            var groupModes = BuildGroupModes(specs);
            var groupNames = new HashSet<string>(groupModes.Keys, StringComparer.Ordinal);

            int removed;
            int ruleCount;
            try
            {
                EditorUtility.DisplayProgressBar("资源自动分组", "清理旧的 Addressables 条目...", 0.1f);
                removed = ClearManagedEntries(settings, groupNames);

                EditorUtility.DisplayProgressBar("资源自动分组", "重建 Addressable Importer 规则...", 0.35f);
                ruleCount = RebuildRules(importSettings, specs, groupModes,
                    separateTemplate, togetherTemplate);

                EnsureGroups(settings, groupModes, separateTemplate, togetherTemplate);

                EditorUtility.SetDirty(importSettings);
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            // 重新导入两个资源根目录，让 Addressable Importer 按新规则创建条目。
            EditorUtility.DisplayProgressBar("资源自动分组", "重新导入资源并建立分组...", 0.6f);
            try
            {
                AddressableImporter.FolderImporter.ReimportFolders(ImportRoots, false);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            int remaining = CountCustomEntries(settings);
            string result =
                $"完成。清理条目 {removed} 个，生成规则 {ruleCount} 条，当前自定义条目 {remaining} 个。\n" +
                "受影响分组：" + string.Join(", ", groupNames.OrderBy(x => x));

            Debug.Log("[AssetsAutoGroup] " + result.Replace('\n', ' '));

            var window = GetWindow<AssetsAutoGroupWindow>();
            window._lastResult = result;
            window.Repaint();
        }

        private static int ClearManagedEntries(AddressableAssetSettings settings, ISet<string> managedGroups)
        {
            var removeGuids = new HashSet<string>(StringComparer.Ordinal);
            var groups = settings.groups.ToArray();

            foreach (var group in groups)
            {
                if (group == null || group.HasSchema<PlayerDataGroupSchema>())
                    continue;

                bool managedGroup = managedGroups.Contains(group.Name);
                foreach (var entry in group.entries.ToArray())
                {
                    if (entry == null)
                        continue;

                    string assetPath = Normalize(entry.AssetPath);
                    bool managedPath = IsUnderManagedRoot(assetPath);
                    bool missingAsset = string.IsNullOrEmpty(assetPath) ||
                                        AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) == null;

                    if (managedGroup || managedPath || missingAsset)
                        removeGuids.Add(entry.guid);
                }
            }

            foreach (var guid in removeGuids)
                settings.RemoveAssetEntry(guid);

            return removeGuids.Count;
        }

        private static int RebuildRules(
            AddressableImportSettings importSettings,
            IReadOnlyList<RuleSpec> specs,
            IReadOnlyDictionary<string, int> groupModes,
            AddressableAssetGroupTemplate separateTemplate,
            AddressableAssetGroupTemplate togetherTemplate)
        {
            var rules = new List<AddressableImportRule>(specs.Count);
            foreach (var spec in specs)
            {
                int packType = groupModes[spec.Group];
                rules.Add(new AddressableImportRule
                {
                    path = BuildPattern(spec.Folder, spec.Extensions),
                    matchType = AddressableImportRuleMatchType.Regex,
                    groupName = spec.Group,
                    groupTemplate = packType == 1 ? separateTemplate : togetherTemplate,
                    groupTemplateApplicationMode = GroupTemplateApplicationMode.AlwaysOverwriteGroupSettings,
                    labelRefs = null,
                    dynamicLabels = null,
                    LabelMode = LabelWriteMode.Add,
                    simplified = false,
                    addressReplacement = string.Empty,
                });
            }

            importSettings.rules.Clear();
            importSettings.rules.AddRange(rules);
            importSettings.rulesEnabled = true;
            importSettings.allowGroupCreation = true;

            var list = AddressableImportSettingsList.Instance;
            if (list != null)
            {
                if (list.SettingList == null)
                    list.SettingList = new List<AddressableImportSettings>();

                if (!list.SettingList.Contains(importSettings))
                {
                    list.SettingList.Add(importSettings);
                    EditorUtility.SetDirty(list);
                }
            }

            return rules.Count;
        }

        private static void EnsureGroups(
            AddressableAssetSettings settings,
            IReadOnlyDictionary<string, int> groupModes,
            AddressableAssetGroupTemplate separateTemplate,
            AddressableAssetGroupTemplate togetherTemplate)
        {
            foreach (var pair in groupModes)
            {
                var template = pair.Value == 1 ? separateTemplate : togetherTemplate;
                var group = settings.FindGroup(pair.Key);
                if (group == null)
                {
                    var schemas = new List<AddressableAssetGroupSchema>();
                    if (settings.DefaultGroup != null && settings.DefaultGroup.Schemas.Count > 0)
                        schemas.Add(settings.DefaultGroup.Schemas[0]);

                    group = settings.CreateGroup(pair.Key, false, false, false,
                        schemas, typeof(BundledAssetGroupSchema));
                }

                if (group == null)
                {
                    Debug.LogError("[AssetsAutoGroup] 创建分组失败: " + pair.Key);
                    continue;
                }

                // 与 Addressable Importer 内部一致：先补齐模板里的 Schema 类型，再套用模板值。
                foreach (var schema in template.SchemaObjects)
                {
                    if (schema != null && !group.HasSchema(schema.GetType()))
                        group.AddSchema(schema.GetType());
                }

                template.ApplyToAddressableAssetGroup(group);
                EditorUtility.SetDirty(group);
            }
        }

        private static int CountCustomEntries(AddressableAssetSettings settings)
        {
            int count = 0;
            foreach (var group in settings.groups)
            {
                if (group == null || group.HasSchema<PlayerDataGroupSchema>())
                    continue;
                count += group.entries.Count;
            }
            return count;
        }

        private static List<RuleSpec> BuildSpecs()
        {
            var specs = new List<RuleSpec>();

            // Map/<MapName>/<子目录>
            foreach (var mapFolder in GetImmediateFolders("Assets/Arts/Map"))
            {
                Add(specs, mapFolder + "/Animation", "Animation", 1, "anim|ani");
                Add(specs, mapFolder + "/Chunk", "MapChunk", 1, "prefab");
                Add(specs, mapFolder + "/Material", "Material", 2, "mat");
                Add(specs, mapFolder + "/Mesh", "Mesh", 2, "asset");
                Add(specs, mapFolder + "/Texture", "Texture", 1, "png|jpg|jpeg");
            }

            // 公共美术目录
            Add(specs, "Assets/Arts/Font", "Font", 1, "");
            Add(specs, "Assets/Arts/Shader", "Shader", 2, "shader|shadervariants");
            Add(specs, "Assets/Arts/SkyBox", "SkyBox", 1, "");

            // FX
            Add(specs, "Assets/Arts/FX/Animation", "Animation", 1, "anim|ani");
            Add(specs, "Assets/Arts/FX/Material", "Material", 2, "mat");
            Add(specs, "Assets/Arts/FX/Mesh", "Mesh", 2, "asset");
            Add(specs, "Assets/Arts/FX/Texture", "Texture", 1, "png|jpg|jpeg");
            Add(specs, "Assets/Arts/FX/Prefab", "FX", 1, "prefab");

            // UI：扫描所有子目录，按目录名归类（Common / Icon / Loading / Login 等都适用）
            foreach (var folder in GetAllFolders("Assets/Arts/UI"))
            {
                string name = Path.GetFileName(folder);
                switch (name)
                {
                    case "Animation":
                        Add(specs, folder, "Animation", 1, "anim|ani");
                        break;
                    case "Material":
                        Add(specs, folder, "Material", 2, "mat");
                        break;
                    case "SingleSprite":
                        Add(specs, folder, "UISprite", 1, "png|jpg|jpeg");
                        break;
                    case "Sprite":
                        Add(specs, folder, "UISprite", 2, "png|jpg|jpeg");
                        break;
                    case "SpriteAtlas":
                        Add(specs, folder, "UISprite", 1, "spriteatlas|spriteatlasv2");
                        break;
                }
            }

            // 打包资源
            Add(specs, "Assets/AssetBundles/GlobalData", "GlobalData", 1, "");
            Add(specs, "Assets/AssetBundles/MapData", "MapData", 1, "asset");
            Add(specs, "Assets/AssetBundles/Prefab", "Prefab", 1, "prefab");
            Add(specs, "Assets/AssetBundles/Scene", "Scene", 1, "unity");
            Add(specs, "Assets/AssetBundles/UI", "UI", 1, "prefab");

            return specs;
        }

        private static Dictionary<string, int> BuildGroupModes(IEnumerable<RuleSpec> specs)
        {
            var modes = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var spec in specs)
            {
                if (!modes.TryGetValue(spec.Group, out int mode) || spec.PackType < mode)
                    modes[spec.Group] = spec.PackType;
            }
            return modes;
        }

        private static List<string> FindPackTypeConflicts(IEnumerable<RuleSpec> specs)
        {
            var result = new List<string>();
            foreach (var group in specs.GroupBy(s => s.Group))
            {
                var modes = group.Select(s => s.PackType).Distinct().OrderBy(x => x).ToArray();
                if (modes.Length > 1)
                {
                    result.Add($"{group.Key}: " + string.Join(" / ", modes.Select(PackTypeName)));
                }
            }
            return result;
        }

        private static void Add(List<RuleSpec> specs, string folder, string group, int packType, string extensions)
        {
            folder = Normalize(folder).TrimEnd('/');
            if (specs.Any(s => s.Folder == folder && s.Group == group && s.Extensions == extensions))
                return;

            specs.Add(new RuleSpec
            {
                Folder = folder,
                Group = group,
                PackType = packType,
                Extensions = extensions,
            });
        }

        private static string BuildPattern(string folder, string extensions)
        {
            string root = "(?i)^" + System.Text.RegularExpressions.Regex.Escape(folder) + "/.*";
            if (string.IsNullOrEmpty(extensions))
                return root + "$";

            return root + "\\.(" + extensions + ")$";
        }

        private static IEnumerable<string> GetImmediateFolders(string root)
        {
            string absolute = ToAbsolutePath(root);
            if (!Directory.Exists(absolute))
                return Array.Empty<string>();

            return Directory.GetDirectories(absolute, "*", SearchOption.TopDirectoryOnly)
                .Select(ToAssetPath)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();
        }

        private static IEnumerable<string> GetAllFolders(string root)
        {
            string absolute = ToAbsolutePath(root);
            if (!Directory.Exists(absolute))
                return Array.Empty<string>();

            return Directory.GetDirectories(absolute, "*", SearchOption.AllDirectories)
                .Select(ToAssetPath)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            return Path.Combine(projectRoot, assetPath);
        }

        private static string ToAssetPath(string absolutePath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            if (!absolutePath.StartsWith(projectRoot, StringComparison.Ordinal))
                return Normalize(absolutePath);

            return Normalize(absolutePath.Substring(projectRoot.Length).TrimStart('/', '\\'));
        }

        private static bool IsUnderManagedRoot(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;

            foreach (var root in ManagedAssetRoots)
            {
                string trimmed = root.TrimEnd('/');
                if (assetPath == trimmed || assetPath.StartsWith(root, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static string Normalize(string path)
        {
            return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/');
        }

        private static string PackTypeName(int packType)
        {
            return packType == 1 ? "PackSeparately" : "PackTogether";
        }
    }
}
