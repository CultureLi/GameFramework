using System.Collections.Generic;
using Framework;
using UnityEngine;

namespace GameMain
{
    /// <summary>
    /// Streams 100×100 terrain chunk prefabs based on the camera's view-frustum footprint on the
    /// ground plane. Each frame we intersect the four screen-corner rays with y = <see cref="groundY"/>
    /// to get a convex quad, then load every block whose XZ AABB overlaps that quad (SAT test).
    /// This naturally scales with camera altitude, tilt, FOV, and aspect ratio — as long as the
    /// camera has ground in view, whatever is visible is loaded.
    /// </summary>
    public class WorldChunkStreaming : MonoBehaviour
    {
        [Header("Camera")]
        public Camera targetCamera;
        [Tooltip("World-space Y of the ground plane used to project the camera frustum.")]
        public float groundY = 0f;

        [Header("Grid")]
        public int gridSize = 10;
        public float blockSize = 100f;
        [Tooltip("World-space origin of block (0,0). Block (i,j) covers [origin + (i,j)*blockSize, origin + (i+1,j+1)*blockSize).")]
        public Vector2 gridOriginXZ = Vector2.zero;

        [Header("Load margin")]
        [Tooltip("Extra world-space padding around the frustum quad. Set to roughly `blockSize` so one ring of blocks just outside the visible footprint is pre-loaded (adjacent-chunk pre-load). Prevents pop-in when the camera slides across a chunk boundary.")]
        public float loadMarginWorld = 100f;
        [Tooltip("Cap the ray→ground intersection distance. Prevents runaway load radius when the camera pitches nearly horizontal so top-screen rays project to infinity on the ground.")]
        public float maxProjectionDistance = 2000f;

        [Header("Addressables")]
        [Tooltip("Prefab path prefix (Addressables key). '{0}' = x index, '{1}' = y index.")]
        public string chunkAddressPattern =
            "Assets/BundleRes/SceneChunk/OpenWorld/OpenWorldTerrainData x({0}) y({1}).prefab";

        [Header("Pool")]
        public string poolName = "OpenWorldChunkPool";
        [Tooltip("Pool capacity — should be ≥ max loaded blocks. Whole 10×10 grid + slack = 128 is safe.")]
        public int poolCapacity = 128;
        public float poolExpireTime = 30f;

        [Header("Timing")]
        [Tooltip("Seconds a block stays loaded after leaving the load set before being pooled back.")]
        public float unloadDelaySeconds = 5f;
        [Tooltip("How often the streaming set is recomputed (seconds). 0 = every frame.")]
        public float refreshInterval = 0.15f;

        [Header("Placement")]
        [Tooltip("If true, spawned chunks are parented to this streaming node (world position stays as authored in the prefab).")]
        public bool parentSpawnedChunks = true;
        [Tooltip("If true, override each chunk's world position to (gridOrigin + (i,j)*blockSize, 0, ...). Leave OFF when TerrainToMesh baked the world position into the prefab.")]
        public bool forcePositionByIndex = false;

        struct BlockKey
        {
            public int x, y;
            public BlockKey(int x, int y) { this.x = x; this.y = y; }
            public override bool Equals(object o) => o is BlockKey k && k.x == x && k.y == y;
            public override int GetHashCode() => (x * 397) ^ y;
        }

        PrefabObjectPool _pool;
        readonly Dictionary<BlockKey, GameObject> _loaded = new Dictionary<BlockKey, GameObject>();
        readonly HashSet<BlockKey> _loading = new HashSet<BlockKey>();
        readonly Dictionary<BlockKey, float> _pendingUnloadAt = new Dictionary<BlockKey, float>();
        readonly HashSet<BlockKey> _wantedThisPass = new HashSet<BlockKey>();
        readonly List<BlockKey> _tmpDue = new List<BlockKey>();
        readonly Vector2[] _quadXZ = new Vector2[4];    // frustum ground-projected quad (CCW when looking down)
        float _quadMinX, _quadMaxX, _quadMinZ, _quadMaxZ;
        float _nextRefreshTime;

        void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
        }

        void Start()
        {
            _pool = PrefabObjectPool.Create(poolName, poolCapacity, poolExpireTime);
        }

        void OnDestroy()
        {
            // Editor stop / app quit: Framework's ObjectPoolMgr may already be disposed by the
            // time our OnDestroy runs, in which case UnSpawn throws "Can not find target in
            // object pool". Skip the pool round-trip entirely on shutdown — Unity/GC handles it.
            if (FrameworkMgr.ShutdownType == EShutdownType.Shutdown)
            {
                _loaded.Clear();
                _loading.Clear();
                _pendingUnloadAt.Clear();
                _pool = null;
                return;
            }

            // Normal runtime teardown (e.g. scene unload): try to return objects to the pool,
            // but swallow benign exceptions if the pool state is unexpectedly gone.
            if (_pool != null)
            {
                foreach (var kv in _loaded)
                {
                    if (kv.Value == null) continue;
                    try { _pool.UnSpawn(kv.Value); }
                    catch { /* pool torn down before us — ignore */ }
                }
                try { _pool.Dispose(); }
                catch { /* ignore */ }
            }
            _loaded.Clear();
            _loading.Clear();
            _pendingUnloadAt.Clear();
            _pool = null;
        }

        void Update()
        {
            if (_pool == null || targetCamera == null) return;
            if (Time.unscaledTime < _nextRefreshTime)
            {
                ProcessPendingUnloads();
                return;
            }
            _nextRefreshTime = Time.unscaledTime + Mathf.Max(0f, refreshInterval);

            ComputeFrustumGroundQuad();
            RecomputeWantedSet();
            LoadMissing();
            MarkOutOfRangeForUnload();
            ProcessPendingUnloads();
        }

        // ---------------------------------------------------------------- Frustum → ground quad

        void ComputeFrustumGroundQuad()
        {
            // Viewport corners in CCW order when looking down: BL → BR → TR → TL.
            // Bottom of screen usually maps to nearer ground points, top to farther.
            var viewportCorners = new[]
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(1f, 0f, 0f),
                new Vector3(1f, 1f, 0f),
                new Vector3(0f, 1f, 0f),
            };
            float capDist = Mathf.Min(maxProjectionDistance, targetCamera.farClipPlane);
            for (int i = 0; i < 4; i++)
            {
                Ray ray = targetCamera.ViewportPointToRay(viewportCorners[i]);
                Vector3 hit;
                if (Mathf.Abs(ray.direction.y) < 1e-6f)
                {
                    hit = ray.origin + ray.direction * capDist;
                }
                else
                {
                    float t = (groundY - ray.origin.y) / ray.direction.y;
                    if (t <= 0f) t = capDist;                              // ray pointing up — clamp to horizon
                    else t = Mathf.Min(t, capDist);                        // ray hits ground far away — clamp
                    hit = ray.origin + ray.direction * t;
                }
                _quadXZ[i] = new Vector2(hit.x, hit.z);
            }
            _quadMinX = _quadMaxX = _quadXZ[0].x;
            _quadMinZ = _quadMaxZ = _quadXZ[0].y;
            for (int i = 1; i < 4; i++)
            {
                if (_quadXZ[i].x < _quadMinX) _quadMinX = _quadXZ[i].x;
                if (_quadXZ[i].x > _quadMaxX) _quadMaxX = _quadXZ[i].x;
                if (_quadXZ[i].y < _quadMinZ) _quadMinZ = _quadXZ[i].y;
                if (_quadXZ[i].y > _quadMaxZ) _quadMaxZ = _quadXZ[i].y;
            }
        }

        // ---------------------------------------------------------------- Wanted set

        void RecomputeWantedSet()
        {
            _wantedThisPass.Clear();

            // Candidate block index range from the quad's AABB, expanded by margin.
            float margin = loadMarginWorld;
            int minI = Mathf.Max(0, Mathf.FloorToInt((_quadMinX - margin - gridOriginXZ.x) / blockSize));
            int maxI = Mathf.Min(gridSize - 1, Mathf.CeilToInt((_quadMaxX + margin - gridOriginXZ.x) / blockSize) - 1);
            int minJ = Mathf.Max(0, Mathf.FloorToInt((_quadMinZ - margin - gridOriginXZ.y) / blockSize));
            int maxJ = Mathf.Min(gridSize - 1, Mathf.CeilToInt((_quadMaxZ + margin - gridOriginXZ.y) / blockSize) - 1);

            for (int j = minJ; j <= maxJ; j++)
                for (int i = minI; i <= maxI; i++)
                    if (BlockOverlapsQuad(i, j))
                        _wantedThisPass.Add(new BlockKey(i, j));
        }

        /// <summary>Convex quad vs axis-aligned block overlap (SAT), with isotropic <see cref="loadMarginWorld"/>.</summary>
        bool BlockOverlapsQuad(int i, int j)
        {
            float bMinX = gridOriginXZ.x + i * blockSize;
            float bMinZ = gridOriginXZ.y + j * blockSize;
            float bMaxX = bMinX + blockSize;
            float bMaxZ = bMinZ + blockSize;
            float m = loadMarginWorld;

            // Box's own axes (X, Z) — cheap AABB overlap test.
            if (_quadMaxX + m < bMinX || _quadMinX - m > bMaxX) return false;
            if (_quadMaxZ + m < bMinZ || _quadMinZ - m > bMaxZ) return false;

            // Quad edge normals (4 axes).
            for (int e = 0; e < 4; e++)
            {
                Vector2 a = _quadXZ[e];
                Vector2 b = _quadXZ[(e + 1) & 3];
                Vector2 n = new Vector2(-(b.y - a.y), b.x - a.x);
                float len = Mathf.Sqrt(n.x * n.x + n.y * n.y);
                if (len < 1e-6f) continue;
                float invLen = 1f / len;
                n.x *= invLen; n.y *= invLen;

                // Project quad
                float qMin = float.MaxValue, qMax = float.MinValue;
                for (int k = 0; k < 4; k++)
                {
                    float p = _quadXZ[k].x * n.x + _quadXZ[k].y * n.y;
                    if (p < qMin) qMin = p;
                    if (p > qMax) qMax = p;
                }
                // Project block AABB (only 4 corners)
                float p1 = bMinX * n.x + bMinZ * n.y;
                float p2 = bMaxX * n.x + bMinZ * n.y;
                float p3 = bMaxX * n.x + bMaxZ * n.y;
                float p4 = bMinX * n.x + bMaxZ * n.y;
                float bMin = p1, bMax = p1;
                if (p2 < bMin) bMin = p2; else if (p2 > bMax) bMax = p2;
                if (p3 < bMin) bMin = p3; else if (p3 > bMax) bMax = p3;
                if (p4 < bMin) bMin = p4; else if (p4 > bMax) bMax = p4;

                // Grow the quad's interval by margin (unit normal → isotropic).
                if (qMax + m < bMin || qMin - m > bMax) return false;
            }
            return true;
        }

        // ---------------------------------------------------------------- Load / unload

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

        void BeginLoad(BlockKey key)
        {
            _loading.Add(key);
            string addr = string.Format(chunkAddressPattern, key.x, key.y);
            _pool.SpawnAsync(addr, go =>
            {
                _loading.Remove(key);
                if (go == null) return;

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

        bool IsWantedNow(BlockKey key)
        {
            // Recompute in case the frame's cached quad went stale during an async load.
            ComputeFrustumGroundQuad();
            return BlockOverlapsQuad(key.x, key.y);
        }

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
        // OnDrawGizmos (not OnDrawGizmosSelected) so gizmos remain visible in Play mode
        // without needing the object selected. Toggle the "Gizmos" button in the Game view
        // to see them at runtime there too.
        void OnDrawGizmos()
        {
            if (targetCamera == null) return;
            // Ensure we have up-to-date data even in Edit mode where Update won't run.
            if (!Application.isPlaying) ComputeFrustumGroundQuad();

            // Full grid (yellow, thin)
            Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
            for (int j = 0; j < gridSize; j++)
                for (int i = 0; i < gridSize; i++)
                {
                    var c = new Vector3(gridOriginXZ.x + (i + 0.5f) * blockSize, groundY, gridOriginXZ.y + (j + 0.5f) * blockSize);
                    Gizmos.DrawWireCube(c, new Vector3(blockSize, 0.1f, blockSize));
                }

            // Loaded / wanted blocks (green, thick)
            Gizmos.color = new Color(0f, 1f, 0f, 1f);
            var wanted = Application.isPlaying ? _wantedThisPass : ComputeEditModeWanted();
            foreach (var k in wanted)
            {
                var c = new Vector3(gridOriginXZ.x + (k.x + 0.5f) * blockSize, groundY + 0.5f, gridOriginXZ.y + (k.y + 0.5f) * blockSize);
                Gizmos.DrawWireCube(c, new Vector3(blockSize * 0.98f, 3f, blockSize * 0.98f));
            }

            // Frustum ground quad (cyan)
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 1f);
            for (int i = 0; i < 4; i++)
            {
                var a = _quadXZ[i];
                var b = _quadXZ[(i + 1) & 3];
                Gizmos.DrawLine(new Vector3(a.x, groundY + 0.2f, a.y), new Vector3(b.x, groundY + 0.2f, b.y));
            }

            // Margin-expanded quad AABB (red dashed-ish outline)
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.85f);
            var cMin = new Vector3(_quadMinX - loadMarginWorld, groundY + 0.3f, _quadMinZ - loadMarginWorld);
            var cMax = new Vector3(_quadMaxX + loadMarginWorld, groundY + 0.3f, _quadMaxZ + loadMarginWorld);
            var center = (cMin + cMax) * 0.5f;
            var size = cMax - cMin; size.y = 0.1f;
            Gizmos.DrawWireCube(center, size);
        }

        // Edit-mode helper: rebuild wanted set on the fly for gizmo visualization
        // (Update won't tick when the game isn't playing).
        HashSet<BlockKey> _editWanted;
        HashSet<BlockKey> ComputeEditModeWanted()
        {
            if (_editWanted == null) _editWanted = new HashSet<BlockKey>();
            _editWanted.Clear();
            float margin = loadMarginWorld;
            int minI = Mathf.Max(0, Mathf.FloorToInt((_quadMinX - margin - gridOriginXZ.x) / blockSize));
            int maxI = Mathf.Min(gridSize - 1, Mathf.CeilToInt((_quadMaxX + margin - gridOriginXZ.x) / blockSize) - 1);
            int minJ = Mathf.Max(0, Mathf.FloorToInt((_quadMinZ - margin - gridOriginXZ.y) / blockSize));
            int maxJ = Mathf.Min(gridSize - 1, Mathf.CeilToInt((_quadMaxZ + margin - gridOriginXZ.y) / blockSize) - 1);
            for (int j = minJ; j <= maxJ; j++)
                for (int i = minI; i <= maxI; i++)
                    if (BlockOverlapsQuad(i, j))
                        _editWanted.Add(new BlockKey(i, j));
            return _editWanted;
        }
#endif
    }
}
