using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace GameFramework.EditorTools.TerrainGen
{
    /// <summary>
    /// 整理 TerrainToMesh 导出的场景资产，把散在同一个文件夹里的预制体 / Mesh / 材质 / 贴图
    /// 拆到 BundleRes 下各自该去的位置，方便按文件夹配 Addressable 分组规则。
    ///
    /// 用法：在场景文件夹（例如 <c>Assets/NoBundleRes/Scene/OpenWorld</c>）上右键 →「整理场景资产」。
    ///
    /// 以文件夹名 <c>OpenWorld</c> 为例，处理流程：
    /// <list type="number">
    /// <item>校验子文件夹 <c>OpenWorldTerrainData</c> 与其中的 <c>OpenWorldTerrainData.prefab</c> 存在，否则终止。</item>
    /// <item>创建或清空 <c>BundleRes/SceneChunk/OpenWorld</c>。</item>
    /// <item>把源预制体里每个 <c>OpenWorldTerrainData x(i) y(j)</c> 节点单独存成预制体，
    ///       放进各自的子文件夹 <c>BundleRes/SceneChunk/OpenWorld/OpenWorldTerrainData x(i) y(j)/</c>。</item>
    /// <item>把每块对应的 <c>... x(i) y(j) Mesh.asset</c> 移到同一个子文件夹里 —— 一块地形的
    ///       预制体和 Mesh 同处一个文件夹，就能整块打成一个 bundle。</item>
    /// <item>创建或清空 <c>BundleRes/Texture/Scene/OpenWorld</c>，把源文件夹里所有贴图移进去。</item>
    /// <item>创建或清空 <c>BundleRes/Material/Scene/OpenWorld</c>，把源文件夹里所有材质移进去。</item>
    /// </list>
    ///
    /// Mesh / 材质 / 贴图一律走 <see cref="AssetDatabase.MoveAsset"/>，GUID 与 localId 都不变，
    /// 所以新旧预制体对它们的引用都不会断。
    /// </summary>
    public static class SceneAssetOrganizer
    {
        /// <summary>TerrainToMesh 的导出子文件夹与主预制体的名字后缀：&lt;文件夹名&gt; + 这个后缀。</summary>
        const string TerrainDataSuffix = "TerrainData";

        /// <summary>地块 Mesh 资源的文件名后缀：&lt;地块名&gt; + 这个后缀。</summary>
        const string MeshAssetSuffix = " Mesh.asset";

        const string ChunkRootFormat = "Assets/BundleRes/SceneChunk/{0}";
        const string TextureRootFormat = "Assets/BundleRes/Texture/Scene/{0}";
        const string MaterialRootFormat = "Assets/BundleRes/Material/Scene/{0}";

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
        [MenuItem("Assets/整理场景资产", true, 1001)]
        static bool ValidateOrganize()
        {
            foreach (string folder in GetSelectedFolders())
                return true;
            return false;
        }

        [MenuItem("Assets/整理场景资产", false, 1001)]
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
                if (!TryBuildPlan(folder, out plan, out error))
                {
                    Debug.LogError("[整理场景资产] 跳过 " + folder + "：" + error);
                    continue;
                }
                plans.Add(plan);
            }
            if (plans.Count == 0) return;

            if (!ConfirmPlans(plans)) return;

            int okCount = 0;
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
                Debug.LogWarning("[整理场景资产] 已取消，注意目标文件夹处于半整理状态，重新执行一次即可。");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log("[整理场景资产] 完成，共处理 " + okCount + " 个场景文件夹。");
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

        /// <summary>一个场景文件夹要做的全部改动，在动手之前先算清楚。</summary>
        class OrganizePlan
        {
            /// <summary>右键选中的场景文件夹，例如 <c>Assets/NoBundleRes/Scene/OpenWorld</c>。</summary>
            public string SceneFolder;
            /// <summary>场景文件夹名，例如 <c>OpenWorld</c>，后面所有目标路径都由它拼出来。</summary>
            public string SceneName;
            /// <summary>TerrainToMesh 的导出子文件夹，例如 <c>.../OpenWorld/OpenWorldTerrainData</c>。</summary>
            public string SourceFolder;
            /// <summary>源主预制体，例如 <c>.../OpenWorldTerrainData/OpenWorldTerrainData.prefab</c>。</summary>
            public string SourcePrefab;

            public string ChunkRoot;
            public string TextureRoot;
            public string MaterialRoot;

            /// <summary>源预制体里所有地块节点的名字，例如 <c>OpenWorldTerrainData x(0) y(0)</c>。</summary>
            public List<string> ChunkNames = new List<string>();
            /// <summary>源文件夹里的贴图资源路径。</summary>
            public List<string> Textures = new List<string>();
            /// <summary>源文件夹里的材质资源路径。</summary>
            public List<string> Materials = new List<string>();
        }

        static bool TryBuildPlan(string sceneFolder, out OrganizePlan plan, out string error)
        {
            plan = null;
            string sceneName = Path.GetFileName(sceneFolder);
            string dataName = sceneName + TerrainDataSuffix;
            string sourceFolder = sceneFolder + "/" + dataName;

            // 需求：子文件夹不存在就直接终止。
            if (!AssetDatabase.IsValidFolder(sourceFolder))
            {
                error = "找不到子文件夹 " + dataName;
                return false;
            }

            string sourcePrefab = sourceFolder + "/" + dataName + ".prefab";
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePrefab);
            if (root == null)
            {
                error = "找不到预制体 " + sourcePrefab;
                return false;
            }

            var p = new OrganizePlan
            {
                SceneFolder = sceneFolder,
                SceneName = sceneName,
                SourceFolder = sourceFolder,
                SourcePrefab = sourcePrefab,
                ChunkRoot = string.Format(ChunkRootFormat, sceneName),
                TextureRoot = string.Format(TextureRootFormat, sceneName),
                MaterialRoot = string.Format(MaterialRootFormat, sceneName),
            };

            // 只读遍历预制体资源本身即可拿到地块名，不必 LoadPrefabContents。
            CollectChunkNames(root.transform, p.ChunkNames);
            if (p.ChunkNames.Count == 0)
            {
                error = "预制体里没有找到形如「<前缀> x(i) y(j)」的地块节点";
                return false;
            }

            foreach (string assetPath in ListAssetFiles(sourceFolder))
            {
                System.Type type = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                if (type == null) continue;
                if (typeof(Texture).IsAssignableFrom(type)) p.Textures.Add(assetPath);
                else if (typeof(Material).IsAssignableFrom(type)) p.Materials.Add(assetPath);
            }

            plan = p;
            error = null;
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
                              + p.Textures.Count + " 张贴图，" + p.Materials.Count + " 个材质）");
                sb.AppendLine("   清空 " + p.ChunkRoot);
                sb.AppendLine("   清空 " + p.TextureRoot);
                sb.AppendLine("   清空 " + p.MaterialRoot);
                sb.AppendLine();
            }
            sb.AppendLine("贴图 / 材质 / Mesh 是【移动】，会从源文件夹消失（GUID 不变，引用不会断）。");
            return EditorUtility.DisplayDialog("整理场景资产", sb.ToString(), "开始整理", "取消");
        }

        // ---------------------------------------------------------------- 执行阶段

        static void Execute(OrganizePlan plan)
        {
            // 1) 创建或清空地块根目录
            CreateOrClearFolder(plan.ChunkRoot);

            // 2)(3) 拆预制体 + 移动 Mesh：两件事都按块来，放在同一个循环里，
            //       保证一块的预制体和它的 Mesh 落在同一个子文件夹。
            int savedPrefabs = 0;
            int movedMeshes = 0;
            var missingMeshes = new List<string>();

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

                    // 该块的 Mesh 资源挪到同一个子文件夹里。
                    string meshSrc = plan.SourceFolder + "/" + chunkName + MeshAssetSuffix;
                    if (AssetDatabase.LoadMainAssetAtPath(meshSrc) == null)
                    {
                        missingMeshes.Add(chunkName + MeshAssetSuffix);
                    }
                    else
                    {
                        string meshDst = chunkFolder + "/" + chunkName + MeshAssetSuffix;
                        string moveError = AssetDatabase.MoveAsset(meshSrc, meshDst);
                        if (string.IsNullOrEmpty(moveError)) movedMeshes++;
                        else Debug.LogError("[整理场景资产] Mesh 移动失败：" + meshSrc + " -> " + meshDst + "  " + moveError);
                    }
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
                EditorUtility.ClearProgressBar();
            }

            // 4) 贴图
            CreateOrClearFolder(plan.TextureRoot);
            int movedTextures = MoveAll(plan.Textures, plan.TextureRoot, "贴图");

            // 5) 材质
            CreateOrClearFolder(plan.MaterialRoot);
            int movedMaterials = MoveAll(plan.Materials, plan.MaterialRoot, "材质");

            // 报告残留：源文件夹里没被认领的资源，避免有东西被静默漏掉。
            var leftover = new List<string>();
            foreach (string assetPath in ListAssetFiles(plan.SourceFolder))
            {
                if (assetPath == plan.SourcePrefab) continue;
                leftover.Add(Path.GetFileName(assetPath));
            }

            var sb = new StringBuilder();
            sb.AppendLine("[整理场景资产] " + plan.SceneName + " 整理完成");
            sb.AppendLine("  地块预制体  " + savedPrefabs + "/" + plan.ChunkNames.Count + "  -> " + plan.ChunkRoot + "/<块名>/<块名>.prefab");
            sb.AppendLine("  地块 Mesh   " + movedMeshes + "/" + plan.ChunkNames.Count + "  -> " + plan.ChunkRoot + "/<块名>/<块名> Mesh.asset");
            sb.AppendLine("  贴图        " + movedTextures + "/" + plan.Textures.Count + "  -> " + plan.TextureRoot);
            sb.AppendLine("  材质        " + movedMaterials + "/" + plan.Materials.Count + "  -> " + plan.MaterialRoot);
            Debug.Log(sb.ToString());

            if (missingMeshes.Count > 0)
                Debug.LogWarning("[整理场景资产] " + plan.SceneName + " 有 " + missingMeshes.Count
                                 + " 块找不到对应的 Mesh 资源：\n  " + string.Join("\n  ", missingMeshes.ToArray()));

            if (leftover.Count > 0)
                Debug.LogWarning("[整理场景资产] " + plan.SceneName + " 源文件夹仍有 " + leftover.Count
                                 + " 个未被整理的资源（既不是贴图也不是材质，也没匹配上任何地块）：\n  "
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

        /// <summary>列出文件夹内一层的资源路径（跳过 .meta，不进子文件夹）。</summary>
        static List<string> ListAssetFiles(string folder)
        {
            var result = new List<string>();
            string abs = Path.Combine(Directory.GetCurrentDirectory(), folder);
            if (!Directory.Exists(abs)) return result;

            foreach (string file in Directory.GetFiles(abs, "*", SearchOption.TopDirectoryOnly))
            {
                if (file.EndsWith(".meta", System.StringComparison.OrdinalIgnoreCase)) continue;
                result.Add(folder + "/" + Path.GetFileName(file));
            }
            return result;
        }
    }
}
