using System.Collections.Generic;
using Framework;
using UnityEngine;

namespace GameMain
{
    /// <summary>
    /// 按相机在地面上的覆盖范围，流式加载 100×100 的地形块预制体。
    ///
    /// 加载区域取自该覆盖范围的轴对齐外接矩形 —— 相机若发布了自己的覆盖范围，就直接用
    /// <see cref="WorldCameraController.TryGetFootprintRect"/>，让流式加载和相机自身的移动限制
    /// 共用同一份「相机能看到哪儿」的定义 —— 再向四边各外扩 <see cref="footprintPadding"/>。
    /// 凡与这个矩形重叠的地块都会被加载。
    ///
    /// 加载区域和地块网格都是轴对齐的，所以「要哪些块」退化成一段纯下标范围，不必逐块求交。
    /// 区域大小仍会自动跟着相机高度、俯角、视场角和宽高比变化，因为覆盖范围本来就由这几项决定。
    /// </summary>
    public class WorldChunkStreaming : MonoBehaviour
    {
        [Header("相机")]
        public Camera targetCamera;
        [Tooltip("投射相机视锥时所用的地面高度（世界空间 Y）。")]
        public float groundY = 0f;

        [Header("网格")]
        [Tooltip("网格边长，单位是块数。10 表示 10×10 共 100 块。")]
        public int gridSize = 10;
        [Tooltip("单个地块的边长（米）。")]
        public float blockSize = 100f;
        [Tooltip("地块 (0,0) 的世界空间原点。地块 (i,j) 覆盖 [原点 + (i,j)*blockSize, 原点 + (i+1,j+1)*blockSize)。")]
        public Vector2 gridOriginXZ = Vector2.zero;

        [Header("加载范围")]
        [Tooltip("在相机覆盖范围的外接矩形基础上，向四边各额外扩张多少米 —— 加载区域就是外扩之后的这个矩形。填成大致等于 blockSize，可以让可见范围外侧一圈地块保持常驻（邻接块预加载），相机跨过块边界时地块就不会凭空弹出来。")]
        public AnimationCurve paddingCurve = AnimationCurve.Linear(50f, 50f, 300f, 500f);
        [Tooltip("回退射线长度。只在相机上没挂 WorldCameraController、需要自己投射时才用得到，而且只用于那些与地平线齐平或朝上、根本打不到地面的视锥角点。真打到地面的角点一律按实际交点距离取值 —— 把它们截短会低估覆盖范围，导致可见的地面没被加载。")]
        public float maxProjectionDistance = 2000f;

        [Header("Addressables")]
        [Tooltip("地块预制体的路径模板（Addressables key）。{0} = x 下标，{1} = y 下标。")]
        public string chunkAddressPattern =
            "Assets/BundleRes/SceneChunk/OpenWorld/OpenWorldTerrainData x({0}) y({1}).prefab";

        [Header("内存池")]
        [Tooltip("内存池名字。")]
        public string poolName = "OpenWorldChunkPool";
        [Tooltip("池容量，应当不小于同时加载的最大块数。整张 10×10 网格再留点余量，128 是安全值。")]
        public int poolCapacity = 128;
        [Tooltip("对象回池后多久过期释放（秒）。")]
        public float poolExpireTime = 30f;

        [Header("时序")]
        [Tooltip("地块离开加载集合之后，还要保留多少秒才回收进池。")]
        public float unloadDelaySeconds = 5f;
        [Tooltip("重算一次加载集合的间隔（秒）。0 = 每帧都算。")]
        public float refreshInterval = 0.15f;

        [Header("摆放")]
        [Tooltip("勾上则把生成出来的地块挂到本流式加载节点下（世界坐标保持预制体里烘好的值）。")]
        public bool parentSpawnedChunks = true;
        [Tooltip("勾上则强行把每个地块的世界坐标改写成 (网格原点 + (i,j)*blockSize)。TerrainToMesh 已经把世界坐标烘进预制体时，保持关闭。")]
        public bool forcePositionByIndex = false;

        /// <summary>地块的网格下标，用作下面各个集合的键。</summary>
        struct BlockKey
        {
            public int x, y;
            public BlockKey(int x, int y) { this.x = x; this.y = y; }
            public override bool Equals(object o) => o is BlockKey k && k.x == x && k.y == y;
            public override int GetHashCode() => (x * 397) ^ y;
        }

        /// <summary>地块预制体的内存池。</summary>
        PrefabObjectPool _pool;
        /// <summary>覆盖范围的来源：相机上的 <see cref="WorldCameraController"/>，可能为空。</summary>
        WorldCameraController _footprintSource;
        /// <summary>已经加载完成的地块。</summary>
        readonly Dictionary<BlockKey, GameObject> _loaded = new Dictionary<BlockKey, GameObject>();
        /// <summary>正在异步加载、还没回调的地块，用来避免重复发起请求。</summary>
        readonly HashSet<BlockKey> _loading = new HashSet<BlockKey>();
        /// <summary>已标记预卸载的地块 → 到期时刻（<see cref="Time.unscaledTime"/>）。</summary>
        readonly Dictionary<BlockKey, float> _pendingUnloadAt = new Dictionary<BlockKey, float>();
        /// <summary>本次刷新算出来的、应当处于加载状态的地块集合。</summary>
        readonly HashSet<BlockKey> _wantedThisPass = new HashSet<BlockKey>();
        /// <summary>遍历字典时不能直接删元素，先把到期的键收集到这里。</summary>
        readonly List<BlockKey> _tmpDue = new List<BlockKey>();
        /// <summary>世界 XZ 上的加载区域（矩形的 X/Y 对应世界的 X/Z）：相机覆盖范围的外接矩形，
        /// 并且已经外扩过 <see cref="footprintPadding"/>。</summary>
        Rect _loadRect;
        /// <summary>下一次重算加载集合的时刻。</summary>
        float _nextRefreshTime;

        /// <summary>视口四角，只给自己投射的回退路径用。既然从它们身上只取一个外接矩形，
        /// 四个角的先后顺序就无关紧要了。</summary>
        static readonly Vector3[] ViewportCorners =
        {
            new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f),
            new Vector3(1f, 1f, 0f), new Vector3(0f, 1f, 0f),
        };

        void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            // 相机自己发布了覆盖范围就优先用它，让流式加载和相机的移动限制
            // 由同一份「相机能看到哪儿」的定义驱动。
            if (targetCamera != null) _footprintSource = targetCamera.GetComponent<WorldCameraController>();
        }

        /// <summary>建好地块预制体的内存池。</summary>
        void Start()
        {
            _pool = PrefabObjectPool.Create(poolName, poolCapacity, poolExpireTime);
        }

        void OnDestroy()
        {
            // 编辑器停止运行 / 程序退出：等我们的 OnDestroy 跑到，Framework 的 ObjectPoolMgr
            // 可能已经先被销毁了，此时 UnSpawn 会抛「Can not find target in object pool」。
            // 关闭流程里干脆不走回池这一步 —— 交给 Unity/GC 处理。
            if (FrameworkMgr.ShutdownType == EShutdownType.Shutdown)
            {
                _loaded.Clear();
                _loading.Clear();
                _pendingUnloadAt.Clear();
                _pool = null;
                return;
            }

            // 正常的运行期销毁（比如卸载场景）：尽量把对象还回池里，
            // 但万一池的状态意外已经没了，就把无害的异常吞掉。
            if (_pool != null)
            {
                foreach (var kv in _loaded)
                {
                    if (kv.Value == null) continue;
                    try { _pool.UnSpawn(kv.Value); }
                    catch { /* 池比我们先销毁 —— 忽略 */ }
                }
                try { _pool.Dispose(); }
                catch { /* 忽略 */ }
            }
            _loaded.Clear();
            _loading.Clear();
            _pendingUnloadAt.Clear();
            _pool = null;
        }

        /// <summary>
        /// 按 <see cref="refreshInterval"/> 的节奏重算加载集合；预卸载的到期检查每帧都做，
        /// 免得回收被刷新间隔拖慢。
        /// </summary>
        void Update()
        {
            if (_pool == null || targetCamera == null) return;
            if (Time.unscaledTime < _nextRefreshTime)
            {
                ProcessPendingUnloads();
                return;
            }
            _nextRefreshTime = Time.unscaledTime + Mathf.Max(0f, refreshInterval);

            ComputeLoadRect();
            CollectBlocksInLoadRect(_wantedThisPass);
            LoadMissing();
            MarkOutOfRangeForUnload();
            ProcessPendingUnloads();
        }

        // ---------------------------------------------------------------- 相机覆盖范围 → 加载矩形

        /// <summary>
        /// 重算 <see cref="_loadRect"/>：取相机地面覆盖范围的轴对齐外接矩形，
        /// 再向四边各外扩 <see cref="footprintPadding"/>。
        /// </summary>
        void ComputeLoadRect()
        {
            if (_footprintSource == null || !_footprintSource.TryGetFootprintRect(out Rect foot))
                foot = ProjectFootprintRect();

            float p = paddingCurve.Evaluate(WorldCameraController.I.ZoomValue);
            _loadRect = Rect.MinMaxRect(foot.xMin - p, foot.yMin - p, foot.xMax + p, foot.yMax + p);
        }

        /// <summary>
        /// 相机上没有 <see cref="WorldCameraController"/> 发布覆盖范围时的独立回退：
        /// 把屏幕四角投到 y = <see cref="groundY"/> 平面上，取它们的外接矩形。
        ///
        /// 打到地面的角点一律按精确交点距离取值；<see cref="maxProjectionDistance"/> 只用于
        /// 那些与地平线齐平或朝上、根本碰不到平面的角点。截断一个真实交点会把该点挪到半空中，
        /// 比它所代表的那片地面更近，于是可见的地形就漏加载了。
        /// </summary>
        Rect ProjectFootprintRect()
        {
            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Ray ray = targetCamera.ViewportPointToRay(ViewportCorners[i]);
                float t;
                if (Mathf.Abs(ray.direction.y) < 1e-6f || ray.direction.y > 0f)
                {
                    t = maxProjectionDistance;                          // 与地平线齐平 / 朝上
                }
                else
                {
                    t = (groundY - ray.origin.y) / ray.direction.y;
                    if (t <= 0f) t = maxProjectionDistance;             // 相机已经在平面之下
                }
                Vector3 hit = ray.origin + ray.direction * t;
                if (hit.x < minX) minX = hit.x;
                if (hit.x > maxX) maxX = hit.x;
                if (hit.z < minZ) minZ = hit.z;
                if (hit.z > maxZ) maxZ = hit.z;
            }
            return Rect.MinMaxRect(minX, minZ, maxX, maxZ);
        }

        // ---------------------------------------------------------------- 加载集合

        /// <summary>
        /// 把与 <see cref="_loadRect"/> 重叠的地块全部填进 <paramref name="into"/>。
        /// 加载区域和网格都是轴对齐的，所以由矩形算出来的下标范围<i>本身</i>就是答案 ——
        /// 范围内的每一块必然重叠，不需要再逐块判定。
        /// </summary>
        void CollectBlocksInLoadRect(HashSet<BlockKey> into)
        {
            into.Clear();
            // 地块 i 覆盖 [i*blockSize, (i+1)*blockSize)，所以取整用 floor / ceil-1：
            // 仅与矩形边相切（重叠面积为零）的地块不算命中。范围为空时下面的循环自然不执行。
            int minI = Mathf.Max(0, Mathf.FloorToInt((_loadRect.xMin - gridOriginXZ.x) / blockSize));
            int maxI = Mathf.Min(gridSize - 1, Mathf.CeilToInt((_loadRect.xMax - gridOriginXZ.x) / blockSize) - 1);
            int minJ = Mathf.Max(0, Mathf.FloorToInt((_loadRect.yMin - gridOriginXZ.y) / blockSize));
            int maxJ = Mathf.Min(gridSize - 1, Mathf.CeilToInt((_loadRect.yMax - gridOriginXZ.y) / blockSize) - 1);

            for (int j = minJ; j <= maxJ; j++)
                for (int i = minI; i <= maxI; i++)
                    into.Add(new BlockKey(i, j));
        }

        /// <summary>
        /// 上面那套判定的单块版本，用于异步加载回调里复查某一个键。
        /// 用严格不等号，让仅与矩形边相切的地块不算重叠，
        /// 与 <see cref="CollectBlocksInLoadRect"/> 的下标范围严格保持一致。
        /// </summary>
        bool BlockOverlapsLoadRect(int i, int j)
        {
            if (i < 0 || j < 0 || i >= gridSize || j >= gridSize) return false;
            float bMinX = gridOriginXZ.x + i * blockSize;
            float bMinZ = gridOriginXZ.y + j * blockSize;
            return _loadRect.xMax > bMinX && _loadRect.xMin < bMinX + blockSize
                && _loadRect.yMax > bMinZ && _loadRect.yMin < bMinZ + blockSize;
        }

        // ---------------------------------------------------------------- 加载 / 卸载

        /// <summary>给本次加载集合里还缺的地块发起加载；已经在的顺手取消它的预卸载。</summary>
        void LoadMissing()
        {
            foreach (var key in _wantedThisPass)
            {
                if (_loaded.ContainsKey(key))
                {
                    _pendingUnloadAt.Remove(key);
                    continue;
                }
                if (_loading.Contains(key)) continue;
                BeginLoad(key);
            }
        }

        /// <summary>从池里异步取一个地块，回调里再确认它是否仍然需要。</summary>
        void BeginLoad(BlockKey key)
        {
            _loading.Add(key);
            string addr = string.Format(chunkAddressPattern, key.x, key.y);
            _pool.SpawnAsync(addr, go =>
            {
                _loading.Remove(key);
                if (go == null) return;

                // 异步这段时间里相机可能已经走开了，不再需要就直接还回池里。
                if (!_wantedThisPass.Contains(key) && !IsWantedNow(key))
                {
                    _pool.UnSpawn(go);
                    return;
                }

                if (parentSpawnedChunks)
                    go.transform.SetParent(transform, worldPositionStays: !forcePositionByIndex);
                if (forcePositionByIndex)
                    go.transform.position = new Vector3(
                        gridOriginXZ.x + key.x * blockSize,
                        0f,
                        gridOriginXZ.y + key.y * blockSize);

                _loaded[key] = go;
            });
        }

        /// <summary>这一块现在还要不要。本帧缓存的矩形可能在异步期间就过期了，所以重算一次。</summary>
        bool IsWantedNow(BlockKey key)
        {
            ComputeLoadRect();
            return BlockOverlapsLoadRect(key.x, key.y);
        }

        /// <summary>
        /// 把已加载、但本次不再需要的地块标记成预卸载，
        /// <see cref="unloadDelaySeconds"/> 秒之后才真正回收。
        /// </summary>
        void MarkOutOfRangeForUnload()
        {
            float dueAt = Time.unscaledTime + unloadDelaySeconds;
            foreach (var kv in _loaded)
            {
                if (_wantedThisPass.Contains(kv.Key)) continue;
                if (!_pendingUnloadAt.ContainsKey(kv.Key))
                    _pendingUnloadAt[kv.Key] = dueAt;
            }
        }

        /// <summary>把所有已经到期的预卸载地块还回池里。</summary>
        void ProcessPendingUnloads()
        {
            if (_pendingUnloadAt.Count == 0) return;
            float now = Time.unscaledTime;
            _tmpDue.Clear();
            foreach (var kv in _pendingUnloadAt)
                if (now >= kv.Value) _tmpDue.Add(kv.Key);

            for (int i = 0; i < _tmpDue.Count; i++)
            {
                var key = _tmpDue[i];
                _pendingUnloadAt.Remove(key);
                if (_loaded.TryGetValue(key, out var go))
                {
                    _loaded.Remove(key);
                    if (go != null) _pool.UnSpawn(go);
                }
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// 用 OnDrawGizmos 而不是 OnDrawGizmosSelected，这样运行期不选中物体也照样能看到。
        /// 想在 Game 视图里看，还得打开右上角的 Gizmos 开关。
        /// </summary>
        void OnDrawGizmos()
        {
            if (targetCamera == null) return;
            // 编辑模式下 Update 不跑，这里自己补算一次，保证画的是最新数据。
            if (!Application.isPlaying) ComputeLoadRect();

            // 整张网格（黄色，细）
            Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
            for (int j = 0; j < gridSize; j++)
                for (int i = 0; i < gridSize; i++)
                {
                    var c = new Vector3(gridOriginXZ.x + (i + 0.5f) * blockSize, groundY, gridOriginXZ.y + (j + 0.5f) * blockSize);
                    Gizmos.DrawWireCube(c, new Vector3(blockSize, 0.1f, blockSize));
                }

            // 需要加载的地块（绿色，粗）
            Gizmos.color = new Color(0f, 1f, 0f, 1f);
            var wanted = Application.isPlaying ? _wantedThisPass : ComputeEditModeWanted();
            foreach (var k in wanted)
            {
                var c = new Vector3(gridOriginXZ.x + (k.x + 0.5f) * blockSize, groundY + 0.5f, gridOriginXZ.y + (k.y + 0.5f) * blockSize);
                Gizmos.DrawWireCube(c, new Vector3(blockSize * 0.98f, 3f, blockSize * 0.98f));
            }

            // 加载矩形本身（红色）：相机覆盖范围的外接矩形外扩 footprintPadding 之后的结果。
            // 它碰到的每一块都会是上面的绿色块，两者应当严格吻合。
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.85f);
            var center = new Vector3(_loadRect.center.x, groundY + 0.3f, _loadRect.center.y);
            Gizmos.DrawWireCube(center, new Vector3(_loadRect.width, 0.1f, _loadRect.height));
        }

        /// <summary>编辑模式下 Gizmos 用的加载集合。游戏没运行时 Update 不会 tick，只能现算一份。</summary>
        HashSet<BlockKey> _editWanted;
        HashSet<BlockKey> ComputeEditModeWanted()
        {
            if (_editWanted == null) _editWanted = new HashSet<BlockKey>();
            CollectBlocksInLoadRect(_editWanted);
            return _editWanted;
        }
#endif
    }
}
