using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Assets.Editor.ShaderVariant
{
    /// <summary>
    /// Shader 变体收集器：扫描项目中的全部材质，按材质所用 shader 所属的
    /// <c>Assets/Arts/Shader/&lt;子目录名&gt;</c> 分桶，逐桶收集变体，
    /// 结果写入 <c>Assets/Arts/Shader/&lt;子目录名&gt;/&lt;子目录名&gt;SVC.shadervariants</c>。
    ///
    /// 整体流程：
    /// 1. 打开一个专用场景 <see cref="SceneAssetPath"/>；
    /// 2. 通过反射调 <c>ShaderUtil.ClearCurrentShaderVariantCollection</c> 清空 Unity 内部的变体 tracker；
    /// 3. 在场景中生成 <see cref="GridSize"/>×<see cref="GridSize"/> 的球体阵列，
    ///    分批把子目录下的材质挂到球上并调用 <c>Camera.Render()</c>；
    /// 4. 等 shader 编译完成后，反射调 <c>ShaderUtil.SaveCurrentShaderVariantCollection</c> 把 tracker 里的变体存到 SVC；
    /// 5. Post-process：把纯 <c>UsePass</c> 壳 shader（tracker 不会记录）显式补进 SVC。
    ///
    /// 此外还会自动检测"只被别的 shader 通过 UsePass/Fallback 引用、没有任何材质直接使用"的
    /// 孤儿 shader，为它们建临时 Material 参与渲染，避免它们的变体丢失。
    /// </summary>
    public static class ShaderVariantCollector
    {
        private const string ShaderRootDir = "Assets/Arts/Shader";
        private const string SceneAssetPath = "Assets/Test/Scene/ShaderVariantTest.unity";
        private const string RootNodeName = "Root";

        private const int GridSize = 6;
        private const float Spacing = 2.0f;

        // Frames to idle between critical steps so Unity's internal shader-variant tracker
        // has time to react to Clear/Render/Save calls.
        private const int FramesAfterClear = 3;
        private const int FramesAfterBatch = 2;
        private const int FramesAfterCompile = 3;
        private const int FramesAfterSave = 3;

        private static HashSet<string> _shaderPathsUsedByMaterials;
        private static Dictionary<string, HashSet<string>> _shaderToShaderDeps;
        private static HashSet<string> _orphanShaderPaths;

        /// <summary>
        /// 当前子目录"允许保留在 SVC 里"的 shader 白名单：本轮真正参与渲染的材质所用的 shader，
        /// 外加它们通过 UsePass/Fallback 传递引用到的 shader。每个子目录开始前由
        /// <see cref="BuildAllowedShaderSet"/> 重建。
        /// 白名单是按 <see cref="Shader"/> 对象而不是路径记录的，所以 URP 包内 shader
        /// （<c>Packages/com.unity.render-pipelines.universal/...</c>）、第三方插件 shader 都能被正常保留。
        /// </summary>
        private static HashSet<Shader> _allowedShaders;

        private static IEnumerator _routine;
        private static bool _prevAsyncCompilation;

        /// <summary>
        /// 菜单入口。做前置校验（Play 模式、目录存在、场景脏检查），
        /// 打开专用测试场景，临时关闭 <see cref="EditorSettings.asyncShaderCompilation"/>，
        /// 然后启动 <see cref="CollectRoutine"/> 协程并注册到 <see cref="EditorApplication.update"/> 逐帧推进。
        /// 重入保护：如果 <see cref="_routine"/> 非空说明正在运行，直接返回。
        /// </summary>
        [MenuItem("Tools/ShaderVariant/Collect Variants Per SubFolder")]
        public static void CollectVariantsPerSubFolder()
        {
            if (_routine != null)
            {
                Debug.LogWarning("[SVC] Collection is already running.");
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[SVC] Cannot collect variants in Play mode.");
                return;
            }

            if (!Directory.Exists(ShaderRootDir))
            {
                Debug.LogError($"[SVC] Shader directory not found: {ShaderRootDir}");
                return;
            }

            var currentScene = SceneManager.GetActiveScene();
            if (currentScene.isDirty)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    Debug.Log("[SVC] Canceled by user.");
                    return;
                }
            }

            var scene = EditorSceneManager.OpenScene(SceneAssetPath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[SVC] Failed to open scene: {SceneAssetPath}");
                return;
            }

            _prevAsyncCompilation = EditorSettings.asyncShaderCompilation;
            EditorSettings.asyncShaderCompilation = false;

            _routine = CollectRoutine();
            EditorApplication.update += Tick;
            Debug.Log("[SVC] Collection started.");
        }

        /// <summary>
        /// <see cref="EditorApplication.update"/> 回调：推进协程一步。
        /// 协程完成或抛异常时都会调 <see cref="Stop"/> 收尾。
        /// </summary>
        private static void Tick()
        {
            try
            {
                if (_routine == null || !_routine.MoveNext())
                {
                    Stop();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Stop();
            }
        }

        /// <summary>
        /// 结束清理：注销 <see cref="EditorApplication.update"/> 回调、
        /// 还原之前的 <see cref="EditorSettings.asyncShaderCompilation"/>、
        /// 刷新一次 AssetDatabase。
        /// </summary>
        private static void Stop()
        {
            EditorApplication.update -= Tick;
            _routine = null;
            EditorSettings.asyncShaderCompilation = _prevAsyncCompilation;
            AssetDatabase.Refresh();
            Debug.Log("[SVC] All subfolder variant collections done.");
        }

        /// <summary>
        /// 主收集协程。以 <see cref="ShaderRootDir"/> 的一级子目录（按材质所用 shader 分桶）为单位循环，
        /// 每个子目录内部按下面的时序运行：
        /// <list type="number">
        /// <item>取得项目中所有使用该目录 shader 的材质 <see cref="CollectMaterialsByShaderFolder"/>。</item>
        /// <item>为孤儿 shader 和目录内未被任何材质使用的 shader 建临时 Material（<see cref="CreateSupplementalMaterials"/>），
        ///       让它们的变体也能被收集到当前子目录的 SVC。</item>
        /// <item>确保子目录对应的 SVC 资源存在 <see cref="EnsureSubFolderCollection"/>，重置 Root 节点。</item>
        /// <item>反射清空 Unity 内部变体 tracker，然后等 <see cref="FramesAfterClear"/> 帧让 Clear 生效。</item>
        /// <item>建球阵、对齐相机，分批把材质挂到球上执行 <c>cam.Render()</c>，每批之间等 <see cref="FramesAfterBatch"/> 帧。</item>
        /// <item>轮询 <see cref="ShaderUtil.anythingCompiling"/> 等 shader 编译完成，然后再多等 <see cref="FramesAfterCompile"/> 帧。</item>
        /// <item>反射把 tracker 落到 .shadervariants 文件，再等 <see cref="FramesAfterSave"/> 帧确保写盘。</item>
        /// <item>Post-process：遍历本轮渲染的所有材质，用 <see cref="EnsureShaderInSVC"/> 补录 tracker 不会自动记录的 UsePass-only 壳 shader。</item>
        /// <item>销毁临时 Material，进入下一个子目录。</item>
        /// </list>
        /// </summary>
        private static IEnumerator CollectRoutine()
        {
            PrepareShaderDependencyData();
            yield return null;

            var materialsByFolder = CollectMaterialsByShaderFolder();
            if (materialsByFolder.Count == 0)
            {
                Debug.LogWarning($"[SVC] No shader folder found under {ShaderRootDir}, nothing to collect.");
                yield break;
            }

            foreach (var pair in materialsByFolder)
            {
                var folderName = pair.Key;
                var materials = pair.Value;
                Debug.Log($"[SVC] [{folderName}] Materials using shaders in '{ShaderRootDir}/{folderName}': {materials.Count}");

                var supplementalMats = CreateSupplementalMaterials(folderName, materials);
                if (supplementalMats.Count > 0)
                {
                    Debug.Log($"[SVC] [{folderName}] Added {supplementalMats.Count} supplemental material(s) for orphan/unused shader variant collection.");
                    materials.AddRange(supplementalMats);
                }

                var svcPath = EnsureSubFolderCollection(folderName);
                var root = PrepareRootNode();

                ClearCurrentShaderVariantCollectionByReflection();
                for (int i = 0; i < FramesAfterClear; i++)
                    yield return null;

                var spheres = CreateSphereGrid(root);
                AlignCamera(spheres);

                var cam = Camera.main;
                int perBatch = spheres.Length;
                int batchCount = Mathf.CeilToInt((float)materials.Count / perBatch);
                for (int b = 0; b < batchCount; b++)
                {
                    int baseIndex = b * perBatch;
                    for (int i = 0; i < perBatch; i++)
                    {
                        var mr = spheres[i].GetComponent<MeshRenderer>();
                        int matIdx = baseIndex + i;
                        if (matIdx < materials.Count)
                        {
                            mr.enabled = true;
                            mr.sharedMaterial = materials[matIdx];
                        }
                        else
                        {
                            mr.enabled = false;
                        }
                    }

                    if (cam != null) cam.Render();
                    SceneView.RepaintAll();
                    Debug.Log($"[SVC] [{folderName}] Batch {b + 1}/{batchCount} rendered ({Mathf.Min(perBatch, materials.Count - baseIndex)} materials).");

                    for (int i = 0; i < FramesAfterBatch; i++) yield return null;
                }

                // Force sync compile is on, but keep a guard just in case.
                while (ShaderUtil.anythingCompiling)
                {
                    yield return null;
                }
                for (int i = 0; i < FramesAfterCompile; i++) yield return null;

                SaveCurrentShaderVariantCollectionByReflection(svcPath);
                for (int i = 0; i < FramesAfterSave; i++) yield return null;

                AssetDatabase.ImportAsset(svcPath, ImportAssetOptions.ForceUpdate);

                var svcAsset = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(svcPath);
                if (svcAsset != null)
                {
                    int removedNonProject = FilterNonProjectShaders(svcAsset);
                    if (removedNonProject > 0)
                    {
                        Debug.Log($"[SVC] [{folderName}] Filtered out {removedNonProject} non-project shader entry(ies) (engine/URP internals).");
                    }

                    var seenShaders = new HashSet<Shader>();
                    int addedCount = 0;
                    foreach (var m in materials)
                    {
                        if (m == null || m.shader == null) continue;
                        if (!seenShaders.Add(m.shader)) continue;
                        if (!IsProjectShader(m.shader)) continue;
                        if (EnsureShaderInSVC(svcAsset, m.shader, out var constructible))
                        {
                            addedCount++;
                            Debug.Log($"[SVC] [{folderName}] Extra-added shader (not auto-tracked): {m.shader.name}");
                        }
                        else if (!constructible)
                        {
                            Debug.LogWarning($"[SVC] [{folderName}] Cannot add shader '{m.shader.name}' — no ShaderVariant is constructible (likely UsePass-only with unresolved pass type).");
                        }
                    }
                    if (addedCount > 0 || removedNonProject > 0)
                    {
                        EditorUtility.SetDirty(svcAsset);
                    }
                }

                AssetDatabase.SaveAssets();
                Debug.Log($"[SVC] [{folderName}] Saved {materials.Count} material(s) variants to: {svcPath}");

                foreach (var m in supplementalMats)
                {
                    if (m != null) UnityEngine.Object.DestroyImmediate(m);
                }

                yield return null;
            }
        }

        private static readonly Regex BlockCommentRegex =
            new Regex(@"/\*[\s\S]*?\*/", RegexOptions.Multiline);
        private static readonly Regex LineCommentRegex =
            new Regex(@"//[^\n]*", RegexOptions.Multiline);
        private static readonly Regex UsePassRegex =
            new Regex(@"\bUsePass\s+""([^""]+)""", RegexOptions.IgnoreCase);
        private static readonly Regex FallbackRegex =
            new Regex(@"\bFallback\s+""([^""]+)""", RegexOptions.IgnoreCase);

        private static readonly PassType[] AllPassTypes =
        {
            PassType.Normal,
            PassType.ScriptableRenderPipeline,
            PassType.ScriptableRenderPipelineDefaultUnlit,
            PassType.ForwardBase,
            PassType.ForwardAdd,
            PassType.Deferred,
            PassType.ShadowCaster,
            PassType.Meta,
            PassType.MotionVectors,
            PassType.LightPrePassBase,
            PassType.LightPrePassFinal,
            PassType.Vertex,
            PassType.VertexLM,
        };

        /// <summary>
        /// 判断一个 shader 是否是"项目自有的"——即资源路径落在 <see cref="ShaderRootDir"/> 下。
        /// 引擎内置 shader（<c>Resources/unity_builtin_extra</c>）、URP 包内 shader（<c>Packages/...</c>）、
        /// 以及项目其它目录下的 shader 都返回 false。
        /// </summary>
        private static bool IsProjectShader(Shader shader)
        {
            if (shader == null) return false;
            var path = AssetDatabase.GetAssetPath(shader);
            if (string.IsNullOrEmpty(path)) return false;
            return path.Replace('\\', '/').StartsWith(ShaderRootDir + "/", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 把 SVC 里所有"非项目 shader"的条目整体剔除。<c>cam.Render()</c> 会连带跑 URP 内部
        /// 管线（BlitCopy、CopyDepth、UberPost、Skybox、编辑器 GUI 等），这些 shader 的变体
        /// 会被 tracker 一起记进去，但它们由 Unity/URP 自己管理，不需要业务侧 SVC 预热，
        /// 保留在 SVC 里反而会污染依赖、增加打包体积。
        /// 通过 <see cref="SerializedObject"/> 直接编辑 <c>m_Shaders</c> 数组来实现（<see cref="ShaderVariantCollection"/>
        /// 公开 API 只能 Add/Remove 单个 variant，无法枚举现有条目）。
        /// </summary>
        /// <returns>被剔除的 shader 条目数。</returns>
        private static int FilterNonProjectShaders(ShaderVariantCollection svc)
        {
            if (svc == null) return 0;
            var so = new SerializedObject(svc);
            var shadersProp = so.FindProperty("m_Shaders");
            if (shadersProp == null || !shadersProp.isArray) return 0;

            int removed = 0;
            for (int i = shadersProp.arraySize - 1; i >= 0; i--)
            {
                var entry = shadersProp.GetArrayElementAtIndex(i);
                var shaderRef = entry.FindPropertyRelative("first");
                var shader = shaderRef != null ? shaderRef.objectReferenceValue as Shader : null;
                if (!IsProjectShader(shader))
                {
                    // ObjectReference 数组元素的 Delete 首次会把引用置空、二次才真正移除。
                    if (shaderRef != null && shaderRef.objectReferenceValue != null)
                    {
                        shaderRef.objectReferenceValue = null;
                    }
                    shadersProp.DeleteArrayElementAtIndex(i);
                    removed++;
                }
            }
            if (removed > 0)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(svc);
            }
            return removed;
        }

        /// <summary>
        /// 把一个 shader 显式塞进 SVC，用于 tracker 不会自动记录的场景。
        /// 典型场景是 UsePass-only 壳 shader（例如 <c>Custom/ReusePass</c> 只写了
        /// <c>UsePass "Custom/SimpleColor/Forward"</c>）——渲染时 Unity 实际执行的是被 UsePass 的那个 pass，
        /// tracker 记录的是被引用的 shader，不是这个壳。此函数依次尝试用
        /// <see cref="AllPassTypes"/> 中的各种 <see cref="PassType"/> 构造合法 <see cref="ShaderVariantCollection.ShaderVariant"/>，
        /// 找到第一个可构造的类型后：<c>Contains</c> 命中说明已在 → 返回 false；否则调 <c>Add</c>。
        /// </summary>
        /// <param name="svc">目标 SVC 资源。</param>
        /// <param name="shader">要塞进去的 shader。</param>
        /// <param name="anyConstructible">是否成功构造出任何一个合法 <c>ShaderVariant</c>。
        /// 若所有 PassType 都构造失败，说明这个 shader 没有可枚举的 pass（极端的空壳），此时会为 false。</param>
        /// <returns>是否新增了一条条目（已在 SVC 里则为 false）。</returns>
        private static bool EnsureShaderInSVC(ShaderVariantCollection svc, Shader shader, out bool anyConstructible)
        {
            anyConstructible = false;
            if (svc == null || shader == null) return false;

            foreach (var pt in AllPassTypes)
            {
                ShaderVariantCollection.ShaderVariant variant;
                try
                {
                    variant = new ShaderVariantCollection.ShaderVariant(shader, pt);
                }
                catch
                {
                    continue;
                }

                anyConstructible = true;

                try
                {
                    if (svc.Contains(variant)) return false;
                }
                catch
                {
                    continue;
                }

                try
                {
                    if (svc.Add(variant)) return true;
                }
                catch
                {
                    continue;
                }
            }
            return false;
        }

        /// <summary>
        /// 一次性预处理：构建三份数据供后续每个子目录使用。
        /// <list type="bullet">
        /// <item><see cref="_shaderPathsUsedByMaterials"/>：项目里被任意 Material 直接引用的 shader 资源路径集合。</item>
        /// <item><see cref="_shaderToShaderDeps"/>：<see cref="ShaderRootDir"/> 下每个 shader → 它通过 UsePass/Fallback
        ///       引用的其它 shader 资源路径集合（通过 <see cref="ParseShaderDependencies"/> 解析源文本得到）。</item>
        /// <item><see cref="_orphanShaderPaths"/>：出现在依赖图里、但没有被任何材质直接使用的孤儿 shader。</item>
        /// </list>
        /// 之所以自己解析源码而不用 <c>AssetDatabase.GetDependencies</c>：UsePass/Fallback 都是按 shader
        /// 名字（<c>Shader "Xxx/Yyy"</c>）在运行时用 <c>Shader.Find</c> 解析的，不会被 AssetDatabase 记为资源依赖。
        /// </summary>
        private static void PrepareShaderDependencyData()
        {
            _shaderPathsUsedByMaterials = new HashSet<string>();
            foreach (var g in AssetDatabase.FindAssets("t:Material"))
            {
                var mPath = AssetDatabase.GUIDToAssetPath(g);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(mPath);
                if (mat == null || mat.shader == null) continue;
                var sPath = AssetDatabase.GetAssetPath(mat.shader);
                if (!string.IsNullOrEmpty(sPath))
                {
                    _shaderPathsUsedByMaterials.Add(sPath);
                }
            }

            _shaderToShaderDeps = new Dictionary<string, HashSet<string>>();
            if (Directory.Exists(ShaderRootDir))
            {
                foreach (var g in AssetDatabase.FindAssets("t:Shader", new[] { ShaderRootDir }))
                {
                    var sPath = AssetDatabase.GUIDToAssetPath(g);
                    if (string.IsNullOrEmpty(sPath) || !sPath.EndsWith(".shader")) continue;
                    _shaderToShaderDeps[sPath] = ParseShaderDependencies(sPath);
                }
            }

            _orphanShaderPaths = new HashSet<string>();
            foreach (var kv in _shaderToShaderDeps)
            {
                foreach (var dep in kv.Value)
                {
                    if (!_shaderPathsUsedByMaterials.Contains(dep))
                    {
                        _orphanShaderPaths.Add(dep);
                    }
                }
            }

            if (_orphanShaderPaths.Count > 0)
            {
                Debug.Log($"[SVC] Detected {_orphanShaderPaths.Count} orphan shader(s) (used only by other shaders via UsePass/Fallback):");
                foreach (var p in _orphanShaderPaths)
                {
                    Debug.Log($"[SVC]   - {p}");
                }
            }
        }

        /// <summary>
        /// 读取一个 .shader 源文件，正则解析 <c>UsePass "SHADERNAME/PASSNAME"</c> 和
        /// <c>Fallback "SHADERNAME"</c>（<c>Fallback Off</c> 忽略），把引用到的 shader 名字通过
        /// <see cref="AddResolvedShader"/> 解析为资源路径。为避免注释里的假引用被误抓，
        /// 会先剥掉 <c>/* ... */</c> 和 <c>// ...</c>。
        /// </summary>
        /// <remarks>
        /// UsePass 的字符串是 <c>"ShaderName/PassName"</c>，但 shader 名字本身可以包含斜杠
        /// （例如 <c>"Custom/SimpleColor"</c>），因此按 **最后一个** <c>/</c> 切分。
        /// </remarks>
        private static HashSet<string> ParseShaderDependencies(string shaderPath)
        {
            var deps = new HashSet<string>();
            string text;
            try
            {
                text = File.ReadAllText(shaderPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SVC] Failed to read shader source: {shaderPath} ({e.Message})");
                return deps;
            }

            text = BlockCommentRegex.Replace(text, string.Empty);
            text = LineCommentRegex.Replace(text, string.Empty);

            foreach (Match m in UsePassRegex.Matches(text))
            {
                var fullRef = m.Groups[1].Value;
                int lastSlash = fullRef.LastIndexOf('/');
                if (lastSlash <= 0) continue;
                var shaderName = fullRef.Substring(0, lastSlash);
                AddResolvedShader(shaderName, shaderPath, deps);
            }

            foreach (Match m in FallbackRegex.Matches(text))
            {
                var shaderName = m.Groups[1].Value;
                if (string.Equals(shaderName, "Off", StringComparison.OrdinalIgnoreCase)) continue;
                AddResolvedShader(shaderName, shaderPath, deps);
            }

            return deps;
        }

        /// <summary>
        /// 小助手：通过 <see cref="Shader.Find"/> 把 shader 名字解析为 <see cref="Shader"/> 对象，
        /// 再拿到其资源路径塞进 <paramref name="deps"/>。跳过内置 shader（路径不指向 <c>.shader</c>
        /// 文件，例如 <c>Resources/unity_builtin_extra</c>）和自引用；解析失败时打 Warning。
        /// </summary>
        /// <param name="shaderName">待解析的 shader 名（<c>Shader "..."</c> 里的字符串）。</param>
        /// <param name="fromPath">发起引用的源 shader 路径，仅用于日志和排除自引用。</param>
        /// <param name="deps">解析成功则加进这个集合。</param>
        private static void AddResolvedShader(string shaderName, string fromPath, HashSet<string> deps)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[SVC] Shader.Find failed for '{shaderName}' referenced by {fromPath}.");
                return;
            }
            var path = AssetDatabase.GetAssetPath(shader);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".shader")) return; // skip built-in
            if (path == fromPath) return;
            deps.Add(path);
        }

        /// <summary>
        /// 为当前 shader 目录补充"没有材质直接使用"的 shader，建 <see cref="HideFlags.DontSave"/> 的临时 Material 参与渲染。
        /// 两个来源：
        /// <list type="number">
        /// <item>孤儿 shader：出现在依赖图里、只被其它 shader 通过 UsePass/Fallback 引用，没有任何材质直接使用。
        ///       从桶内材质的 shader 出发 BFS 遍历 <see cref="_shaderToShaderDeps"/>，覆盖
        ///       "材质 → shader A → 孤儿 B → 孤儿 C" 这种链式引用。</item>
        /// <item>目录内未被任何材质使用的 shader：保证 <see cref="ShaderRootDir"/> 下每个 shader
        ///       都能被收集到所属目录的 SVC 里，即使美术还没建对应材质。</item>
        /// </list>
        /// 两条来源按 <see cref="Shader"/> 去重；非项目 shader（URP/引擎内置）会被跳过，因为它们最终会被
        /// <see cref="FilterNonProjectShaders"/> 从 SVC 里剔除，渲染它们只是浪费编译时间。
        /// </summary>
        /// <param name="folderName">当前 shader 子目录名（<see cref="ShaderRootDir"/> 下的一级目录）。</param>
        /// <param name="materials">当前桶内真实加载出来的材质。</param>
        /// <returns>需要额外渲染的临时 Material 列表；调用方用完必须 <c>DestroyImmediate</c>。</returns>
        private static List<Material> CreateSupplementalMaterials(string folderName, List<Material> materials)
        {
            var result = new List<Material>();
            var covered = new HashSet<Shader>();
            foreach (var m in materials)
            {
                if (m == null || m.shader == null) continue;
                covered.Add(m.shader);
            }

            // 来源 1：孤儿 shader。
            if (_orphanShaderPaths != null && _orphanShaderPaths.Count > 0)
            {
                var needed = new HashSet<string>();
                var visited = new HashSet<string>();
                var queue = new Queue<string>();
                foreach (var m in materials)
                {
                    if (m == null || m.shader == null) continue;
                    var sPath = AssetDatabase.GetAssetPath(m.shader);
                    if (!string.IsNullOrEmpty(sPath)) queue.Enqueue(sPath);
                }

                while (queue.Count > 0)
                {
                    var cur = queue.Dequeue();
                    if (!visited.Add(cur)) continue;
                    if (!_shaderToShaderDeps.TryGetValue(cur, out var deps)) continue;
                    foreach (var d in deps)
                    {
                        if (_orphanShaderPaths.Contains(d)) needed.Add(d);
                        if (!visited.Contains(d)) queue.Enqueue(d);
                    }
                }

                foreach (var p in needed)
                {
                    var shader = AssetDatabase.LoadAssetAtPath<Shader>(p);
                    if (shader == null) continue;
                    // 变体按 shader 自己所在的目录归档：其它目录的孤儿由它自己的桶负责收集，
                    // 这里再收一遍会在多个 SVC 里产生重复条目。
                    if (!string.Equals(GetShaderBucket(shader), folderName, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!covered.Add(shader)) continue;
                    result.Add(CreateTempMaterial(shader, "__SVC_Orphan_"));
                }
            }

            // 来源 2：本目录下没有被任何材质使用的 shader。
            foreach (var shaderPath in GetShaderPathsInFolder(folderName))
            {
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
                if (shader == null) continue;
                if (!covered.Add(shader)) continue;
                result.Add(CreateTempMaterial(shader, "__SVC_Unused_"));
                Debug.Log($"[SVC] [{folderName}] Shader has no material, using a temporary one: {shaderPath}");
            }

            return result;
        }

        /// <summary>建一个不参与保存的临时材质，<see cref="HideFlags.DontSave"/> 保证不会写进场景/资产。</summary>
        private static Material CreateTempMaterial(Shader shader, string namePrefix)
        {
            return new Material(shader)
            {
                hideFlags = HideFlags.DontSave,
                name = namePrefix + shader.name
            };
        }

        /// <summary>
        /// 确保 <c>{ShaderRootDir}/{folderName}/{folderName}SVC.shadervariants</c> 存在。
        /// 目录不存在则创建；SVC 资源不存在则新建一个空的 <see cref="ShaderVariantCollection"/>。
        /// </summary>
        /// <returns>SVC 资源的相对路径。</returns>
        private static string EnsureSubFolderCollection(string folderName)
        {
            var shaderSubDir = $"{ShaderRootDir}/{folderName}";
            if (!Directory.Exists(shaderSubDir))
            {
                Directory.CreateDirectory(shaderSubDir);
            }

            var svcPath = $"{shaderSubDir}/{folderName}SVC.shadervariants";
            if (AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(svcPath) == null)
            {
                var svc = new ShaderVariantCollection { name = $"{folderName}SVC" };
                AssetDatabase.CreateAsset(svc, svcPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[SVC] Created: {svcPath}");
            }

            return svcPath;
        }

        /// <summary>
        /// 找到（或新建）场景里的根节点 <c>Root</c>，并清空它下面的所有子节点，
        /// 保证每个子目录的渲染从空场景开始。
        /// </summary>
        private static GameObject PrepareRootNode()
        {
            var root = GameObject.Find(RootNodeName);
            if (root == null)
            {
                root = new GameObject(RootNodeName);
            }

            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            }

            return root;
        }

        /// <summary>
        /// 扫描项目中全部材质，按"材质所用 shader 所属的 <see cref="ShaderRootDir"/> 一级子目录"分桶。
        /// 桶名即 SVC 的子目录名，返回结果按桶名排序，保证每次收集顺序稳定。
        ///
        /// 过滤规则：
        /// <list type="bullet">
        /// <item>跳过 <c>Packages/</c> 下的材质（URP/TMP 等第三方包自带材质不需要预热）。</item>
        /// <item>跳过 shader 不在 <see cref="ShaderRootDir"/>/&lt;子目录&gt;/ 下的材质 ——
        ///       它们的变体最终会被 <see cref="FilterNonProjectShaders"/> 剔除，渲染没有意义。</item>
        /// <item>即使某个子目录下没有任何材质，也会加入结果（空列表），
        ///       以便 <see cref="CreateSupplementalMaterials"/> 为目录内的 shader 建临时材质。</item>
        /// </list>
        /// </summary>
        private static SortedDictionary<string, List<Material>> CollectMaterialsByShaderFolder()
        {
            var result = new SortedDictionary<string, List<Material>>(StringComparer.OrdinalIgnoreCase);
            int skipped = 0;

            foreach (var g in AssetDatabase.FindAssets("t:Material"))
            {
                var matPath = NormalizePath(AssetDatabase.GUIDToAssetPath(g));
                if (string.IsNullOrEmpty(matPath)) continue;
                if (matPath.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase)) continue;

                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null || mat.shader == null) continue;

                var folderName = GetShaderBucket(mat.shader);
                if (string.IsNullOrEmpty(folderName))
                {
                    skipped++;
                    continue;
                }

                if (!result.TryGetValue(folderName, out var list))
                {
                    list = new List<Material>();
                    result.Add(folderName, list);
                }
                list.Add(mat);
            }

            foreach (var folderName in GetShaderSubFolders())
            {
                if (!result.ContainsKey(folderName))
                {
                    result.Add(folderName, new List<Material>());
                }
            }

            if (skipped > 0)
            {
                Debug.Log($"[SVC] Skipped {skipped} material(s) whose shader is not under '{ShaderRootDir}/<SubFolder>/'.");
            }

            return result;
        }

        /// <summary>
        /// 取 <see cref="ShaderRootDir"/> 下的所有一级子目录名。只返回至少包含一个 .shader 的目录，
        /// 避免对仅有 SVC 的目录重复建空集合。
        /// </summary>
        private static IEnumerable<string> GetShaderSubFolders()
        {
            if (!Directory.Exists(ShaderRootDir)) yield break;

            foreach (var dir in Directory.GetDirectories(ShaderRootDir))
            {
                var name = Path.GetFileName(dir);
                if (string.IsNullOrEmpty(name) || name.StartsWith(".", StringComparison.Ordinal)) continue;
                if (!GetShaderPathsInFolder(name).Any()) continue;
                yield return name;
            }
        }

        /// <summary>
        /// 列出 <c>{ShaderRootDir}/{folderName}</c> 下（含更深子目录）的所有 .shader 资源路径。
        /// </summary>
        private static IEnumerable<string> GetShaderPathsInFolder(string folderName)
        {
            var folder = $"{ShaderRootDir}/{folderName}";
            if (!AssetDatabase.IsValidFolder(folder)) yield break;

            foreach (var g in AssetDatabase.FindAssets("t:Shader", new[] { folder }))
            {
                var p = NormalizePath(AssetDatabase.GUIDToAssetPath(g));
                if (string.IsNullOrEmpty(p)) continue;
                if (!p.EndsWith(".shader", StringComparison.OrdinalIgnoreCase)) continue;
                yield return p;
            }
        }

        /// <summary>
        /// 取 shader 资源路径在 <see cref="ShaderRootDir"/> 下的一级子目录名；
        /// shader 不在该目录下、或直接放在根目录（没有子目录）时返回 null。
        /// </summary>
        private static string GetShaderBucket(Shader shader)
        {
            if (shader == null) return null;
            var path = NormalizePath(AssetDatabase.GetAssetPath(shader));
            var prefix = ShaderRootDir + "/";
            if (string.IsNullOrEmpty(path) || !path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return null;

            var relative = path.Substring(prefix.Length);
            int slash = relative.IndexOf('/');
            if (slash <= 0) return null;
            return relative.Substring(0, slash);
        }

        /// <summary>统一成正斜杠，避免 Windows 下 <c>Directory</c> 与 <c>AssetDatabase</c> 斜杠混用。</summary>
        private static string NormalizePath(string path)
        {
            return string.IsNullOrEmpty(path) ? path : path.Replace('\\', '/');
        }

        /// <summary>
        /// 在 <paramref name="root"/> 下建 <see cref="GridSize"/>×<see cref="GridSize"/> 的球体阵列，
        /// 球间距为 <see cref="Spacing"/>，整体以原点为中心。移除自带的 <see cref="Collider"/>。
        /// 这些球在批渲染时会依次被赋不同 sharedMaterial 用来触发 shader 编译/变体记录。
        /// </summary>
        private static GameObject[] CreateSphereGrid(GameObject root)
        {
            int total = GridSize * GridSize;
            var spheres = new GameObject[total];
            float offset = (GridSize - 1) * 0.5f * Spacing;

            for (int i = 0; i < total; i++)
            {
                int x = i % GridSize;
                int y = i / GridSize;

                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = $"Sphere_{i:D3}";
                sphere.transform.SetParent(root.transform, false);
                sphere.transform.localPosition = new Vector3(x * Spacing - offset, y * Spacing - offset, 0);

                var collider = sphere.GetComponent<Collider>();
                if (collider != null) UnityEngine.Object.DestroyImmediate(collider);

                spheres[i] = sphere;
            }

            return spheres;
        }

        /// <summary>
        /// 根据球阵整体尺寸 + 相机 fov/aspect，把 <see cref="Camera.main"/> 摆到能框住整片球阵的位置。
        /// 分别按垂直和水平方向算所需距离 <c>distanceV</c> / <c>distanceH</c>，取大值再加 1 单位余量。
        /// 场景里没有 <c>MainCamera</c> tag 的相机时只打 Warning 不报错，后续 <c>cam.Render()</c> 会被跳过。
        /// </summary>
        private static void AlignCamera(GameObject[] spheres)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[SVC] No MainCamera found in scene.");
                return;
            }

            float halfExtent = (GridSize - 1) * 0.5f * Spacing + 1.0f;
            float halfVFov = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float tanHalfV = Mathf.Tan(halfVFov);
            float distanceV = halfExtent / tanHalfV;
            float aspect = Mathf.Max(cam.aspect, 0.0001f);
            float distanceH = halfExtent / (tanHalfV * aspect);
            float distance = Mathf.Max(distanceV, distanceH) + 1.0f;

            cam.transform.position = new Vector3(0, 0, -distance);
            cam.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = Mathf.Max(1000f, distance * 4f);
        }

        /// <summary>
        /// 反射调用 <c>UnityEditor.ShaderUtil.ClearCurrentShaderVariantCollection()</c>（internal 方法），
        /// 清空 Unity 内部记录"当前会话渲染过的 shader 变体"的 tracker。
        /// 该 API 没有公开接口，需要通过反射调；找不到方法则打 Error。
        /// </summary>
        private static void ClearCurrentShaderVariantCollectionByReflection()
        {
            var method = typeof(ShaderUtil).GetMethod(
                "ClearCurrentShaderVariantCollection",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (method == null)
            {
                Debug.LogError("[SVC] Reflection failed: ShaderUtil.ClearCurrentShaderVariantCollection not found.");
                return;
            }

            method.Invoke(null, null);
            Debug.Log("[SVC] Current shader variant collection cleared.");
        }

        /// <summary>
        /// 反射调用 <c>UnityEditor.ShaderUtil.SaveCurrentShaderVariantCollection(string path)</c>（internal 方法），
        /// 把当前 tracker 里的变体序列化到 <paramref name="assetPath"/> 指向的 .shadervariants 文件。
        /// 该 API 没有公开接口，需要通过反射调；找不到方法则打 Error。
        /// 若目标目录不存在会先创建，避免 Unity 端写盘失败。
        /// </summary>
        private static void SaveCurrentShaderVariantCollectionByReflection(string assetPath)
        {
            var method = typeof(ShaderUtil).GetMethod(
                "SaveCurrentShaderVariantCollection",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (method == null)
            {
                Debug.LogError("[SVC] Reflection failed: ShaderUtil.SaveCurrentShaderVariantCollection not found.");
                return;
            }

            var dir = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            method.Invoke(null, new object[] { assetPath });
        }
    }
}
