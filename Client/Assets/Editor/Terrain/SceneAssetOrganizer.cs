using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameFramework.EditorTools.TerrainGen
{
    /// <summary>
    /// 整理 TerrainToMesh 导出的场景资产：按【资源类型】把场景文件夹下散落的资源移动到
    /// <c>Assets/Arts/Map/&lt;场景名&gt;/</c> 下对应的文件夹，方便按文件夹配 Addressable 分组规则。
    ///
    /// 类型 → 目录映射：
    /// <list type="bullet">
    /// <item>Mesh      → <c>Assets/Arts/Map/&lt;场景名&gt;/Mesh</c></item>
    /// <item>Material  → <c>Assets/Arts/Map/&lt;场景名&gt;/Material</c></item>
    /// <item>Texture   → <c>Assets/Arts/Map/&lt;场景名&gt;/Texture</c></item>
    /// <item>Shader    → <c>Assets/Arts/Shader/&lt;场景名&gt;</c>（不放在 Map 下）</item>
    /// <item>地块预制体 → <c>Assets/Arts/Map/&lt;场景名&gt;/Chunk/&lt;块名&gt;/&lt;块名&gt;.prefab</c></item>
    /// </list>
    ///
    /// 用法：
    ///   1. 在场景文件夹（例如 <c>Assets/AssetNoBundles/Scene/OpenWorld</c>）上右键 →「整理场景资产」；
    ///   2. 或者打开场景后，在 MapEditor 窗口点击「整理当前场景资产」。
    ///
    /// 以场景 <c>Assets/AssetNoBundles/Scene/OpenWorld/OpenWorld.unity</c> 为例，处理流程：
    /// <list type="number">
    /// <item>校验子文件夹 <c>OpenWorldTerrainData</c> 与其中的 <c>OpenWorldTerrainData.prefab</c> 存在，否则终止。</item>
    /// <item>以场景文件夹为根【递归扫描】，按 <see cref="AssetDatabase.GetMainAssetTypeAtPath"/> 的
    ///       主资源类型归类，而不是按文件名或目录名猜。</item>
    /// <item>创建或清空 Chunk / Mesh / Material / Texture / Shader 五个目标目录。</item>
    /// <item>把源预制体里每个 <c>OpenWorldTerrainData x(i) y(j)</c> 节点单独存成预制体，
    ///       放进 <c>.../Chunk/OpenWorldTerrainData x(i) y(j)/</c>。</item>
    /// <item>其余资源按类型整批 <see cref="AssetDatabase.MoveAsset"/> 到对应目录。</item>
    /// </list>
    ///
    /// 所有移动都走 <see cref="AssetDatabase.MoveAsset"/>，GUID 与 localId 都不变，
    /// 所以新旧预制体对它们的引用都不会断。未被识别的资源会留在原位并在结束时汇总提示。
    /// </summary>
    public static class SceneAssetOrganizer
    {
        /// <summary>TerrainToMesh 的导出子文件夹与主预制体的名字后缀：&lt;文件夹名&gt; + 这个后缀。</summary>
        const string TerrainDataSuffix = "TerrainData";

        const string ChunkRootFormat = "Assets/Arts/Map/{0}/Chunk";
        const string MeshRootFormat = "Assets/Arts/Map/{0}/Mesh";
        const string TextureRootFormat = "Assets/Arts/Map/{0}/Texture";
        const string MaterialRootFormat = "Assets/Arts/Map/{0}/Material";
        /// <summary>Shader 不进 Map 目录，而是按场景名放到 <c>Assets/Arts/Shader/&lt;场景名&gt;</c>。</summary>
        const string ShaderRootFormat = "Assets/Arts/Shader/{0}";

        /// <summary>
        /// 地块节点的命名规则：<c>&lt;前缀&gt; x(数字) y(数字)</c>。
        /// 用正则匹配而不是按固定层级去找，是因为 TerrainToMesh 的容器节点名字带计数后缀
        /// （实际叫 <c>Mesh (Count 100)</c> 而不是 <c>Mesh</c>），层级名不可靠。
        /// 注意 LOD 子节点名字形如 <c>... x(0) y(0) LOD0</c>，结尾多了 LOD 段，不会被误匹配。
        /// </summary>
        static readonly Regex ChunkNameRx =
            new Regex(@"^(?<prefix>.+) x\((?<x>\d+)\) y\((?<y>\d+)\)$", RegexOptions.Compiled);

        // ---------------------------------------------------------------- 菜单入口

        /// <summary>只有选中了文件夹才让菜单项可点。</summary>
        //[MenuItem("Assets/整理场景资产", true, 1001)]
        static bool ValidateOrganize()
        {
            foreach (string folder in GetSelectedFolders())
                return true;
            return false;
        }

        //[MenuItem("Assets/整理场景资产", false, 1001)]
        static void Organize()
        {
            var folders = GetSelectedFolders();
            if (folders.Count == 0)
            {
                Debug.LogError("[整理场景资产] 需要选择文件夹。");
                return;
            }

            // ---- 先做只读校验并把要干的活儿算出来，全部通过后再确认、再动手。
            // 清空目标文件夹是不可逆的，所以绝不能边校验边删。
            var plans = new List<OrganizePlan>();
            foreach (string folder in folders)
            {
                OrganizePlan plan;
                string error;
                if (!TryBuildPlan(folder, Path.GetFileName(folder), out plan, out error))
                {
                    Debug.LogError("[整理场景资产] 跳过 " + folder + "：" + error);
                    continue;
                }
                plans.Add(plan);
            }

            OrganizePlans(plans);
        }

        /// <summary>
        /// 当前打开场景对应的整理信息。目录名按场景文件名推导；
        /// 场景未保存到 Assets 下时返回 false 且 info 为 null。
        /// </summary>
        public static bool TryGetCurrentSceneInfo(out SceneAssetOrganizeInfo info)
        {
            info = null;

            var scene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path))
                return false;

            string sceneFolder = Path.GetDirectoryName(scene.path);
            if (string.IsNullOrEmpty(sceneFolder))
                return false;

            sceneFolder = sceneFolder.Replace('\\', '/');
            if (!sceneFolder.StartsWith("Assets", System.StringComparison.Ordinal))
                return false;

            string sceneName = Path.GetFileNameWithoutExtension(scene.path);
            if (string.IsNullOrEmpty(sceneName))
                sceneName = Path.GetFileName(sceneFolder);

            return TryGetSceneInfo(sceneFolder, sceneName, scene.path, out info);
        }

        /// <summary>整理当前打开的场景，所有目录都由场景路径推导。</summary>
        public static bool OrganizeCurrentScene()
        {
            if (!TryGetCurrentSceneInfo(out var info))
            {
                Debug.LogError("[整理场景资产] 当前场景无法确定资产目录：" +
                               (info != null ? info.Error : "场景未保存到 Assets 下"));
                return false;
            }

            OrganizePlan plan;
            string error;
            if (!TryBuildPlan(info.SceneFolder, info.SceneName, out plan, out error))
            {
                Debug.LogError("[整理场景资产] " + error);
                return false;
            }

            return OrganizePlans(new List<OrganizePlan> { plan });
        }

        /// <summary>确认并执行已经构建好的整理方案。</summary>
        static bool OrganizePlans(List<OrganizePlan> plans)
        {
            if (plans.Count == 0)
                return false;

            if (!ConfirmPlans(plans))
                return false;

            int okCount = 0;
            bool canceled = false;
            try
            {
                foreach (var plan in plans)
                {
                    Execute(plan);
                    okCount++;
                }
            }
            catch (System.OperationCanceledException)
            {
                // 用户点了进度条上的取消。已经改掉的部分保留原样，不做回滚。
                canceled = true;
                Debug.LogWarning("[整理场景资产] 已取消，注意目标文件夹处于半整理状态，重新执行一次即可。");
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log("[整理场景资产] 完成，共处理 " + okCount + " 个场景文件夹。");
            return !canceled && okCount == plans.Count;
        }

        static List<string> GetSelectedFolders()
        {
            var result = new List<string>();
            foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path))
                    result.Add(path);
            }
            return result;
        }

        // ---------------------------------------------------------------- 方案（只读阶段）

        /// <summary>
        /// 由场景路径推导出的整理信息，只做只读校验，不修改任何资产。
        /// MapEditor 窗口用它展示源目录、输出目录和处理数量。
        /// </summary>
        public sealed class SceneAssetOrganizeInfo
        {
            public string ScenePath;
            /// <summary>场景所在文件夹。</summary>
            public string SceneFolder;
            /// <summary>场景文件名（不含扩展名），所有输出目录都由它命名。</summary>
            public string SceneName;
            public string SourceFolder;
            public string SourcePrefab;
            public string ChunkRoot;
            public string MeshRoot;
            public string TextureRoot;
            public string MaterialRoot;
            /// <summary>场景专属的 shader 目录，位于 <c>Assets/Arts/Shader/&lt;场景名&gt;</c>。</summary>
            public string ShaderRoot;

            public readonly List<string> ChunkNames = new List<string>();
            public readonly List<string> Meshes = new List<string>();
            public readonly List<string> Textures = new List<string>();
            public readonly List<string> Materials = new List<string>();
            public readonly List<string> Shaders = new List<string>();

            public string Error;

            public bool IsValid
            {
                get { return string.IsNullOrEmpty(Error); }
            }
        }

        /// <summary>一个场景文件夹要做的全部改动，在动手之前先算清楚。</summary>
        class OrganizePlan
        {
            /// <summary>右键选中的场景文件夹，例如 <c>Assets/NoBundleRes/Scene/OpenWorld</c>。</summary>
            public string SceneFolder;
            /// <summary>场景文件名，例如 <c>OpenWorld</c>，后面所有目标路径都由它拼出来。</summary>
            public string SceneName;
            /// <summary>TerrainToMesh 的导出子文件夹，例如 <c>.../OpenWorld/OpenWorldTerrainData</c>。</summary>
            public string SourceFolder;
            /// <summary>源主预制体，例如 <c>.../OpenWorldTerrainData/OpenWorldTerrainData.prefab</c>。</summary>
            public string SourcePrefab;

            public string ChunkRoot;
            public string MeshRoot;
            public string TextureRoot;
            public string MaterialRoot;
            public string ShaderRoot;

            /// <summary>源预制体里所有地块节点的名字，例如 <c>OpenWorldTerrainData x(0) y(0)</c>。</summary>
            public List<string> ChunkNames = new List<string>();
            /// <summary>场景文件夹下递归扫描到的 Mesh 资源路径。</summary>
            public List<string> Meshes = new List<string>();
            /// <summary>场景文件夹下递归扫描到的贴图资源路径。</summary>
            public List<string> Textures = new List<string>();
            /// <summary>场景文件夹下递归扫描到的材质资源路径。</summary>
            public List<string> Materials = new List<string>();
            /// <summary>场景文件夹下递归扫描到的 Shader 资源路径。</summary>
            public List<string> Shaders = new List<string>();
        }

        static bool TryBuildPlan(string sceneFolder, string sceneName,
            out OrganizePlan plan, out string error)
        {
            plan = null;

            if (!TryGetSceneInfo(sceneFolder, sceneName, null, out var info))
            {
                error = info != null ? info.Error : "无法读取场景目录";
                return false;
            }

            plan = new OrganizePlan
            {
                SceneFolder = sceneFolder,
                SceneName = sceneName,
                SourceFolder = info.SourceFolder,
                SourcePrefab = info.SourcePrefab,
                ChunkRoot = info.ChunkRoot,
                MeshRoot = info.MeshRoot,
                TextureRoot = info.TextureRoot,
                MaterialRoot = info.MaterialRoot,
                ShaderRoot = info.ShaderRoot,
                ChunkNames = info.ChunkNames,
                Meshes = info.Meshes,
                Textures = info.Textures,
                Materials = info.Materials,
                Shaders = info.Shaders,
            };
            error = null;
            return true;
        }

        /// <summary>
        /// 根据场景文件夹名与场景名推导所有目录，并做只读校验：
        /// TerrainToMesh 的导出子文件夹、主预制体、地块节点都存在才算通过。
        /// </summary>
        static bool TryGetSceneInfo(string sceneFolder, string sceneName,
            string scenePath, out SceneAssetOrganizeInfo info)
        {
            info = new SceneAssetOrganizeInfo
            {
                ScenePath = scenePath,
                SceneFolder = sceneFolder,
                SceneName = sceneName,
            };

            string dataName = sceneName + TerrainDataSuffix;
            info.SourceFolder = sceneFolder + "/" + dataName;
            info.SourcePrefab = info.SourceFolder + "/" + dataName + ".prefab";
            info.ChunkRoot = string.Format(ChunkRootFormat, sceneName);
            info.MeshRoot = string.Format(MeshRootFormat, sceneName);
            info.TextureRoot = string.Format(TextureRootFormat, sceneName);
            info.MaterialRoot = string.Format(MaterialRootFormat, sceneName);
            info.ShaderRoot = string.Format(ShaderRootFormat, sceneName);

            if (!AssetDatabase.IsValidFolder(sceneFolder))
            {
                info.Error = "找不到场景文件夹 " + sceneFolder;
                return false;
            }

            // 需求：子文件夹不存在就直接终止。
            if (!AssetDatabase.IsValidFolder(info.SourceFolder))
            {
                info.Error = "找不到子文件夹 " + dataName;
                return false;
            }

            var root = AssetDatabase.LoadAssetAtPath<GameObject>(info.SourcePrefab);
            if (root == null)
            {
                info.Error = "找不到预制体 " + info.SourcePrefab;
                return false;
            }

            // 只读遍历预制体资源本身即可拿到地块名，不必 LoadPrefabContents。
            CollectChunkNames(root.transform, info.ChunkNames);
            if (info.ChunkNames.Count == 0)
            {
                info.Error = "预制体里没有找到形如「<前缀> x(i) y(j)」的地块节点";
                return false;
            }

            // 以场景文件夹为根递归扫描，按主资源类型归类。TerrainToMesh 常把 Mesh / 材质
            // 放在以地形命名的兄弟目录（例如 "New Terrain/"）里，只看 TerrainData 子目录会漏。
            info.Meshes.AddRange(CollectAssets<Mesh>(sceneFolder));
            info.Materials.AddRange(CollectAssets<Material>(sceneFolder));
            info.Textures.AddRange(CollectAssets<Texture>(sceneFolder));
            info.Shaders.AddRange(CollectAssets<Shader>(sceneFolder));

            return true;
        }

        /// <summary>
        /// 递归收集地块节点名。匹配上的节点不再往下钻 —— 它内部的 LODs / LOD0 之类都属于这一块。
        /// </summary>
        static void CollectChunkNames(Transform t, List<string> into)
        {
            for (int i = 0; i < t.childCount; i++)
            {
                Transform c = t.GetChild(i);
                if (ChunkNameRx.IsMatch(c.name)) into.Add(c.name);
                else CollectChunkNames(c, into);
            }
        }

        static bool ConfirmPlans(List<OrganizePlan> plans)
        {
            var sb = new StringBuilder();
            sb.AppendLine("即将整理以下场景资产。下列目标文件夹会被【清空重建】，其中已有的内容会被删除：");
            sb.AppendLine();
            foreach (var p in plans)
            {
                sb.AppendLine("▍" + p.SceneName + "（" + p.ChunkNames.Count + " 块地形，"
                              + p.Meshes.Count + " 个 Mesh，" + p.Materials.Count + " 个材质，"
                              + p.Textures.Count + " 张贴图，" + p.Shaders.Count + " 个 Shader）");
                sb.AppendLine("   清空 " + p.ChunkRoot);
                sb.AppendLine("   清空 " + p.MeshRoot);
                sb.AppendLine("   清空 " + p.MaterialRoot);
                sb.AppendLine("   清空 " + p.TextureRoot);
                sb.AppendLine("   清空 " + p.ShaderRoot);
                sb.AppendLine();
            }
            sb.AppendLine("Mesh / 材质 / 贴图 / Shader 是【移动】，会从源文件夹消失（GUID 不变，引用不会断）。");
            return EditorUtility.DisplayDialog("整理场景资产", sb.ToString(), "开始整理", "取消");
        }

        // ---------------------------------------------------------------- 执行阶段

        static void Execute(OrganizePlan plan)
        {
            // 1) 创建或清空地块根目录
            CreateOrClearFolder(plan.ChunkRoot);

            // 2) 拆预制体：每个地块节点单独存成预制体。
            int savedPrefabs = 0;

            GameObject contents = PrefabUtility.LoadPrefabContents(plan.SourcePrefab);
            try
            {
                // 先把节点收集成列表，再逐个改父子关系 —— 边遍历边改会漏。
                var chunks = new List<Transform>();
                CollectChunks(contents.transform, chunks);

                for (int i = 0; i < chunks.Count; i++)
                {
                    Transform chunk = chunks[i];
                    string chunkName = chunk.name;

                    if (EditorUtility.DisplayCancelableProgressBar(
                            "整理场景资产 - " + plan.SceneName,
                            "导出地块 " + chunkName + "  (" + (i + 1) + "/" + chunks.Count + ")",
                            (float)i / chunks.Count))
                    {
                        throw new System.OperationCanceledException("用户取消");
                    }

                    string chunkFolder = plan.ChunkRoot + "/" + chunkName;
                    EnsureFolder(chunkFolder);

                    // 提到预览场景的根上，世界坐标保持不变：TerrainToMesh 已经把每块的世界坐标
                    // 烘进了 localPosition，容器和根又都是单位变换，所以存出来的预制体
                    // 根节点就带着正确的位置，和 WorldChunkStreaming 里 forcePositionByIndex=false 的约定一致。
                    chunk.SetParent(null, true);

                    bool ok;
                    PrefabUtility.SaveAsPrefabAsset(chunk.gameObject, chunkFolder + "/" + chunkName + ".prefab", out ok);
                    if (ok) savedPrefabs++;
                    else Debug.LogError("[整理场景资产] 预制体保存失败：" + chunkName, contents);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
                EditorUtility.ClearProgressBar();
            }

            // 3) 按资源类型移动。
            //    Mesh / 材质 / 贴图 -> Assets/Arts/Map/<场景名>/<类型>
            //    Shader             -> Assets/Arts/Shader/<场景名>
            CreateOrClearFolder(plan.MeshRoot);
            int movedMeshes = MoveAll(plan.Meshes, plan.MeshRoot, "Mesh");

            CreateOrClearFolder(plan.MaterialRoot);
            int movedMaterials = MoveAll(plan.Materials, plan.MaterialRoot, "材质");

            CreateOrClearFolder(plan.TextureRoot);
            int movedTextures = MoveAll(plan.Textures, plan.TextureRoot, "贴图");

            CreateOrClearFolder(plan.ShaderRoot);
            int movedShaders = MoveAll(plan.Shaders, plan.ShaderRoot, "Shader");

            // 报告残留：场景文件夹里没被认领的资源，避免有东西被静默漏掉。
            var leftover = new List<string>();
            foreach (string assetPath in ListAssetFilesRecursive(plan.SceneFolder))
            {
                if (assetPath == plan.SourcePrefab) continue;
                if (assetPath.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase)) continue;
                leftover.Add(assetPath.Substring(plan.SceneFolder.Length).TrimStart('/'));
            }

            var sb = new StringBuilder();
            sb.AppendLine("[整理场景资产] " + plan.SceneName + " 整理完成");
            sb.AppendLine("  地块预制体  " + savedPrefabs + "/" + plan.ChunkNames.Count + "  -> " + plan.ChunkRoot + "/<块名>/<块名>.prefab");
            sb.AppendLine("  Mesh        " + movedMeshes + "/" + plan.Meshes.Count + "  -> " + plan.MeshRoot);
            sb.AppendLine("  材质        " + movedMaterials + "/" + plan.Materials.Count + "  -> " + plan.MaterialRoot);
            sb.AppendLine("  贴图        " + movedTextures + "/" + plan.Textures.Count + "  -> " + plan.TextureRoot);
            sb.AppendLine("  Shader      " + movedShaders + "/" + plan.Shaders.Count + "  -> " + plan.ShaderRoot);
            Debug.Log(sb.ToString());

            if (leftover.Count > 0)
                Debug.LogWarning("[整理场景资产] " + plan.SceneName + " 场景文件夹仍有 " + leftover.Count
                                 + " 个未被整理的资源（不是 Mesh / 材质 / 贴图 / Shader，也不是地块预制体）：\n  "
                                 + string.Join("\n  ", leftover.ToArray()));
        }

        /// <summary>递归收集地块节点，规则同 <see cref="CollectChunkNames"/>。</summary>
        static void CollectChunks(Transform t, List<Transform> into)
        {
            for (int i = 0; i < t.childCount; i++)
            {
                Transform c = t.GetChild(i);
                if (ChunkNameRx.IsMatch(c.name)) into.Add(c);
                else CollectChunks(c, into);
            }
        }

        static int MoveAll(List<string> sources, string dstFolder, string label)
        {
            int moved = 0;
            for (int i = 0; i < sources.Count; i++)
            {
                string src = sources[i];
                string dst = dstFolder + "/" + Path.GetFileName(src);
                string error = AssetDatabase.MoveAsset(src, dst);
                if (string.IsNullOrEmpty(error)) moved++;
                else Debug.LogError("[整理场景资产] " + label + "移动失败：" + src + " -> " + dst + "  " + error);
            }
            return moved;
        }

        // ---------------------------------------------------------------- 文件夹 / 路径工具

        /// <summary>创建文件夹；已存在则先整个删掉再建，确保是干净的。</summary>
        static void CreateOrClearFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder) && !AssetDatabase.DeleteAsset(folder))
                throw new IOException("[整理场景资产] 无法清空文件夹（可能有文件被占用）：" + folder);
            EnsureFolder(folder);
        }

        /// <summary>按层级逐段补齐缺失的文件夹。</summary>
        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        /// <summary>
        /// 递归收集 <paramref name="root"/> 下某一类主资源（Mesh / Material / Texture / Shader …）。
        /// 按 <see cref="AssetDatabase.FindAssets"/> 的类型过滤，而不是看扩展名或目录名 ——
        /// TerrainToMesh 常把 Mesh 导到以地形命名的兄弟目录里，按目录名猜会漏。
        /// </summary>
        static List<string> CollectAssets<T>(string root) where T : Object
        {
            var result = new List<string>();
            if (!AssetDatabase.IsValidFolder(root)) return result;

            var seen = new HashSet<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');
                if (string.IsNullOrEmpty(path) || !seen.Add(path)) continue;
                if (AssetDatabase.LoadMainAssetAtPath(path) is T) result.Add(path);
            }

            result.Sort(System.StringComparer.Ordinal);
            return result;
        }

        /// <summary>递归列出文件夹内的资源路径（跳过 .meta），返回以 <c>Assets/</c> 开头的相对路径。</summary>
        static List<string> ListAssetFilesRecursive(string folder)
        {
            var result = new List<string>();
            string projectRoot = Directory.GetCurrentDirectory().TrimEnd('\\', '/').Replace('\\', '/');
            string abs = Path.Combine(projectRoot, folder);
            if (!Directory.Exists(abs)) return result;

            string rootWithSlash = projectRoot + "/";
            foreach (string file in Directory.GetFiles(abs, "*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".meta", System.StringComparison.OrdinalIgnoreCase)) continue;
                string full = file.Replace('\\', '/');
                if (!full.StartsWith(rootWithSlash, System.StringComparison.OrdinalIgnoreCase)) continue;
                result.Add(full.Substring(rootWithSlash.Length));
            }

            result.Sort(System.StringComparer.Ordinal);
            return result;
        }
    }
}
