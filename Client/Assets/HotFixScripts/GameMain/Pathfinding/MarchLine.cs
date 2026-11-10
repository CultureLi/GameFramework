using System.Collections.Generic;
using UnityEngine;

namespace GameMain
{
    /// <summary>
    /// 行军线（从 client3 的 Scripts/Map/Troop/Scene/MarchLine.cs 移植）。
    ///
    /// 用 Mesh 沿路径生成一条带宽度的带子，UV 的 V 方向按路径长度铺开；
    /// MarchLineView.shader 通过 _SpeedV 滚动 UV，形成虚线/箭头向前流动的效果。
    /// 起点/终点指示器和终点脉冲动画由预制体上的 SpriteRenderer / Animation 提供。
    ///
    /// 相比原版：
    ///   - 去掉了地图区域类型（陆地/水域）和多材质拆分，只使用 _land；
    ///   - 去掉了 PooledList / FW.MaterialSpawner 依赖，改用普通 List 和运行时 Material；
    ///   - 增加 SetPath，用于客户端寻路时每帧更新“起点跟随角色”的路径。
    /// </summary>
    public class MarchLine : MonoBehaviour
    {
        [SerializeField] private List<SpriteRenderer> _ends;
        [SerializeField] private Material _land;
        [SerializeField] private Material _water;

        public Color color = Color.white;
        public float width = 1f;
        public float textureSizeRatio = 1f;
        public float speed = 1f;
        public bool smooth = true;
        public float space = 0f;
        public SpriteRenderer startIndicator;
        public SpriteRenderer endIndicator;

        public float Length { get; private set; }

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private float _maxV;
        private bool _active = true;
        private bool _reverseSpeed;
        private Material[] _cachedMaterials;

        private static readonly int _speedVShaderID = Shader.PropertyToID("_SpeedV");
        private static readonly int _addWidthID = Shader.PropertyToID("_AddWidth");
        private static readonly int _widthID = Shader.PropertyToID("_Width");
        private static readonly int _spaceVID = Shader.PropertyToID("_SpaceV");
        private static readonly int _percentID = Shader.PropertyToID("_Percent");

        private readonly List<Vector3> _path = new List<Vector3>();
        private readonly List<Vector3> _smoothedPath = new List<Vector3>();
        private readonly List<Vector3> _positions = new List<Vector3>();
        private readonly List<Vector2> _uvs = new List<Vector2>();
        private readonly List<Color> _colors = new List<Color>();
        private readonly List<Vector3> _normals = new List<Vector3>();
        private readonly List<int> _indices = new List<int>();

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshFilter = GetComponent<MeshFilter>();
            if (_meshFilter != null)
                _meshFilter.mesh = new Mesh();
        }

        /// <summary>
        /// 初始化一条完整的行军线。path 使用世界坐标，第一个点为起点，最后一个点为终点。
        /// </summary>
        public void Initialize(List<Vector3> path, bool reverseSpeed = false)
        {
            _reverseSpeed = reverseSpeed;
            ResetMaterial();
            transform.position = new Vector3(0f, 0.02f, 0f);
            SetPath(path);
        }

        /// <summary>
        /// 更新路径并重建网格，不重建材质。寻路过程中每帧更新起点时调用。
        /// </summary>
        public void SetPath(List<Vector3> path)
        {
            _path.Clear();
            if (path != null)
                _path.AddRange(path);

            SetIndicators(_path);
            BuildMesh(_path, smooth);
        }

        public void SetSpeed(float value)
        {
            speed = value;
            if (_cachedMaterials == null)
                return;

            for (int i = 0; i < _cachedMaterials.Length; i++)
                if (_cachedMaterials[i] != null)
                    _cachedMaterials[i].SetFloat(_speedVShaderID, speed * (_reverseSpeed ? -1f : 1f));
        }

        public void SetSpaceV(float value)
        {
            space = value;
            if (_cachedMaterials == null)
                return;

            for (int i = 0; i < _cachedMaterials.Length; i++)
                if (_cachedMaterials[i] != null)
                    _cachedMaterials[i].SetFloat(_spaceVID, space);
        }

        public void AddWidth(float addWidth)
        {
            if (_cachedMaterials == null)
                return;

            for (int i = 0; i < _cachedMaterials.Length; i++)
                if (_cachedMaterials[i] != null)
                    _cachedMaterials[i].SetFloat(_addWidthID, addWidth);

            if (startIndicator != null)
                startIndicator.transform.localScale = (1f + addWidth) * 5f * width * Vector3.one;
            if (endIndicator != null)
                endIndicator.transform.localScale = (1f + addWidth) * 5f * width * Vector3.one;
        }

        /// <summary>
        /// 进度裁剪（0=全部显示，1=全部隐藏）。当前寻路表现没有使用，保留原版接口。
        /// </summary>
        public void UpdateProgress(float progress)
        {
            if (_cachedMaterials == null)
                return;

            for (int i = 0; i < _cachedMaterials.Length; i++)
            {
                var material = _cachedMaterials[i];
                if (material != null)
                    material.SetFloat(_percentID, (1f - progress) * _maxV);
            }
        }

        public void SetActive(bool active)
        {
            if (_active == active)
                return;

            _active = active;
            gameObject.SetActive(active);
        }

        private void SetIndicators(List<Vector3> points)
        {
            if (points != null && points.Count > 0)
            {
                if (startIndicator != null)
                {
                    startIndicator.gameObject.SetActive(true);
                    startIndicator.transform.position = points[0];
                    startIndicator.color = color;
                    startIndicator.transform.localScale = Vector3.one * 5f * width;
                }
            }
            else if (startIndicator != null)
            {
                startIndicator.gameObject.SetActive(false);
            }

            if (endIndicator == null)
                return;

            if (points == null || points.Count < 2)
            {
                endIndicator.gameObject.SetActive(false);
                return;
            }

            endIndicator.gameObject.SetActive(true);
            endIndicator.transform.position = points[points.Count - 1];
            endIndicator.color = color;
            endIndicator.transform.localScale = Vector3.one * 5f * width;

            if (_ends != null)
            {
                for (int i = 0; i < _ends.Count; i++)
                    if (_ends[i] != null)
                        _ends[i].color = color;
            }
        }

        private void BuildMesh(List<Vector3> source, bool useSmooth)
        {
            if (_meshFilter == null || _meshRenderer == null)
                return;

            var points = useSmooth ? SmoothPath(source) : source;
            EnsureMaterial();

            var mesh = _meshFilter.sharedMesh;
            if (mesh == null)
            {
                mesh = new Mesh();
                _meshFilter.sharedMesh = mesh;
            }

            mesh.Clear();
            mesh.MarkDynamic();
            mesh.subMeshCount = 1;

            Length = GetLength(points);
            float tileLength = Mathf.Max(0.0001f, width / Mathf.Max(0.0001f, textureSizeRatio));
            _maxV = Length / tileLength;

            if (points == null || points.Count < 2)
            {
                if (startIndicator != null)
                    startIndicator.gameObject.SetActive(false);
                return;
            }

            _positions.Clear();
            _uvs.Clear();
            _colors.Clear();
            _normals.Clear();
            _indices.Clear();

            float lineLengthSoFar = 0f;
            var lineColor = color;

            for (int i = 0; i < points.Count; i++)
            {
                int idx = i;
                int cur = i;
                int next = i + 1;

                if (i == 0)
                {
                    AddPosition();
                }
                else
                {
                    if (i == points.Count - 1)
                    {
                        next = points.Count - 1;
                        cur = next - 1;
                    }

                    AddPosition();

                    int lastVertexIndex = _positions.Count - 1;
                    int a = lastVertexIndex - 3;
                    int b = lastVertexIndex - 2;
                    int c = lastVertexIndex - 1;
                    int d = lastVertexIndex;

                    _indices.Add(a);
                    _indices.Add(b);
                    _indices.Add(c);
                    _indices.Add(b);
                    _indices.Add(d);
                    _indices.Add(c);
                }

                void AddPosition()
                {
                    float halfWidth = width * 0.5f;

                    var dir = points[next] - points[cur];
                    float length = dir.magnitude;
                    if (length > 1e-5f)
                        dir /= length;

                    if (idx != 0 && idx != points.Count - 1)
                    {
                        var prevDir = points[idx] - points[idx - 1];
                        if (prevDir.sqrMagnitude > 1e-10f)
                            prevDir.Normalize();

                        float angle = Vector3.Angle(dir, -prevDir) / 2f;
                        float sin = Mathf.Sin(angle * Mathf.Deg2Rad);
                        if (sin >= 0.5f)
                            halfWidth /= sin;

                        dir += prevDir;
                        if (dir.sqrMagnitude > 1e-10f)
                            dir.Normalize();
                    }

                    float v = _reverseSpeed
                        ? _maxV - lineLengthSoFar / tileLength
                        : lineLengthSoFar / tileLength;

                    var perpDir = new Vector3(dir.z, 0f, -dir.x);
                    _positions.Add(points[idx] + perpDir * halfWidth);
                    _positions.Add(points[idx] - perpDir * halfWidth);

                    _colors.Add(lineColor);
                    _colors.Add(lineColor);

                    _normals.Add(perpDir.normalized);
                    _normals.Add(-perpDir.normalized);

                    _uvs.Add(new Vector2(0f, v));
                    _uvs.Add(new Vector2(1f, v));
                    lineLengthSoFar += length;
                }
            }

            mesh.SetVertices(_positions);
            mesh.SetUVs(0, _uvs);
            mesh.SetColors(_colors);
            mesh.SetNormals(_normals);
            mesh.SetIndices(_indices, MeshTopology.Triangles, 0);
        }

        /// <summary>圆滑拐角：在拐点两侧按宽度插入辅助点，避免斜接处 UV 拉伸。</summary>
        private List<Vector3> SmoothPath(List<Vector3> path)
        {
            _smoothedPath.Clear();

            if (path == null || path.Count <= 2)
            {
                if (path != null)
                    _smoothedPath.AddRange(path);
                return _smoothedPath;
            }

            float twoWidthSqr = width * width * 4f;
            _smoothedPath.Add(path[0]);

            for (int i = 1; i < path.Count - 1; i++)
            {
                var curPoint = path[i];
                var prevPoint = path[i - 1];
                var nextPoint = path[i + 1];

                var prevDir = prevPoint - curPoint;
                var nextDir = nextPoint - curPoint;

                float prevDirSqrMag = prevDir.sqrMagnitude;
                float nextDirSqrMag = nextDir.sqrMagnitude;

                prevDir.Normalize();
                nextDir.Normalize();

                var cornerDir = (prevDir + nextDir).normalized;
                float dot = Vector3.Dot(prevDir, nextDir);
                var newCurPoint = curPoint + (dot + 1f) * 0.5f * width * cornerDir;

                if (prevDirSqrMag > twoWidthSqr)
                    _smoothedPath.Add(curPoint + prevDir * width);

                _smoothedPath.Add(newCurPoint);

                if (nextDirSqrMag > twoWidthSqr)
                    _smoothedPath.Add(curPoint + nextDir * width);
            }

            _smoothedPath.Add(path[path.Count - 1]);
            return _smoothedPath;
        }

        private void EnsureMaterial()
        {
            if (_cachedMaterials != null && _cachedMaterials.Length > 0 &&
                _cachedMaterials[0] != null)
            {
                var existing = _cachedMaterials[0];
                existing.SetFloat(_addWidthID, 0f);
                existing.SetFloat(_widthID, width);
                existing.SetFloat(_spaceVID, space);
                existing.SetFloat(_speedVShaderID, speed * (_reverseSpeed ? -1f : 1f));
                return;
            }

            if (_land == null)
            {
                Debug.LogError("[MarchLine] 没有配置陆地材质 _land");
                return;
            }

            var material = new Material(_land) { name = "MarchLine (Runtime)" };
            material.SetFloat(_addWidthID, 0f);
            material.SetFloat(_widthID, width);
            material.SetFloat(_spaceVID, space);
            material.SetFloat(_speedVShaderID, speed * (_reverseSpeed ? -1f : 1f));
            material.SetFloat(_percentID, 0f);

            _cachedMaterials = new[] { material };
            _meshRenderer.sharedMaterials = _cachedMaterials;
        }

        private void ResetMaterial()
        {
            if (_cachedMaterials == null)
                return;

            for (int i = 0; i < _cachedMaterials.Length; i++)
            {
                var material = _cachedMaterials[i];
                if (material == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(material);
                else
                    DestroyImmediate(material);
            }

            _cachedMaterials = null;
        }

        public static float GetLength(List<Vector3> points)
        {
            if (points == null)
                return 0f;

            float length = 0f;
            for (int i = 0; i < points.Count - 1; i++)
                length += Vector3.Distance(points[i], points[i + 1]);

            return length;
        }

        private void OnDestroy()
        {
            ResetMaterial();
        }
    }
}
