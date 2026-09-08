using Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameMain
{
    /// <summary>
    /// 锚定在地面上的地图相机。真正的状态是 <see cref="ViewCenter"/>（相机在地面上注视的世界坐标点）
    /// 加上一个归一化的 <see cref="ZoomValue"/>；Transform 每帧都由这两者推导出来。
    /// 先算注视点、再算 Transform（而不是直接搬动 Transform），是"让某个地面点始终贴在鼠标下面"
    /// 对拖动和滚轮都能精确成立的原因，同时也让高度变化不会把画面带偏。
    ///
    /// 输入：按住鼠标某个键拖动地面来平移视野，快速甩动后松手会进入惯性滑行；滚轮缩放。
    /// 相机的 XZ 会被约束，使其地面覆盖范围符合地图矩形 —— 见 <see cref="CameraBoundType"/>。
    ///
    /// 视角和视场角都是固定的：俯角恒为 <see cref="pitchDegrees"/>（默认 45°），视场角一律沿用
    /// Camera 组件上配置的值。缩放<i>只</i>改变高度，拖动和滚轮都碰不到旋转和镜头。
    ///
    /// 相机高度不在本组件上配置，而是由 <see cref="CameraZoomCurve.heightCurve"/> 直接给出米数。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class WorldCameraController : SingletonMono<WorldCameraController>
    {
        /// <summary>缩放曲线整个缺失时退回的高度，只是为了不让相机贴在地面上。</summary>
        const float kFallbackHeight = 50f;

        [Header("缩放")]
        [Tooltip("每格滚轮对应的高度变化（米）。内部会按高度曲线的取值跨度换算成归一化 0→1 缩放值上的步长，所以重新调整曲线的高低范围后，这里的手感不会跟着走样。")]
        public float scrollSensitivity = 60f;
        [Tooltip("缩放值的平滑时间（秒）。0 = 立即到位。平滑的是缩放*值*而不是高度，这样所有由缩放派生出来的量（高度、边界外扩量）才会作为一个整体一起变化。")]
        public float heightSmoothTime = 0.15f;
        [Tooltip("滚轮缩放时把鼠标下方的地面点钉住，于是缩放是朝着光标所指的地方推进，而不是朝着屏幕中心。")]
        public bool zoomTowardCursor = true;

        [Header("缩放曲线")]
        [Tooltip("缩放值 → 相机高度（米）。这是相机高度的唯一来源；视场角和俯角刻意不受缩放影响。")]
        public CameraZoomCurve zoomCurve = new CameraZoomCurve();

        [Header("拖动平移")]
        [Tooltip("用于拖动地面的鼠标按键。0=左键，1=右键，2=中键。")]
        public int dragMouseButton = 0;
        [Tooltip("用于拖动射线检测和视锥投影的地面平面世界 Y 坐标。")]
        public float groundY = 0f;
        [Tooltip("按下后光标需要移动多少像素才算进入平移。对应 EventSystem.pixelDragThreshold 的作用，让抖了一两个像素的点击不会把相机拖歪。")]
        public float dragThresholdPixels = 6f;

        [Header("惯性")]
        [Tooltip("快速拖动后松手，画面会继续滑行，并按衰减曲线减速。")]
        public bool inertiaEnabled = true;
        [Tooltip("触发滑行所需的松手速度，单位是「每秒多少屏地面」。用屏幕单位而不是米来衡量，可以让任意高度下的甩动手感保持一致。")]
        public float inertiaMinScreenSpeed = 0.15f;
        [Tooltip("松手速度的上限，单位同上（每秒多少屏）。")]
        public float inertiaMaxScreenSpeed = 2f;
        [Tooltip("滑行持续时长（秒）。")]
        public float inertiaDuration = 1f;
        [Tooltip("滑行期间的速度倍率，横轴 0→1 对应整段滑行时长。")]
        public AnimationCurve inertiaDecay = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Header("视角（固定 —— 永不被输入改变）")]
        [Tooltip("向下俯角（度）。拖动和滚轮都不会碰旋转。运行时修改这个值会立刻生效，方便实时调角度。")]
        [Range(5f, 89f)] public float pitchDegrees = 45f;
        [Tooltip("水平朝向（度）。同样是固定的。")]
        public float yawDegrees = 0f;

        [Header("地图边界")]
        [Tooltip("为 true 时约束相机 XZ，使其地面覆盖范围符合地图矩形。")]
        public bool clampToMapBounds = true;
        [Tooltip("Inbound（内切）：可见地面始终留在地图内 —— 开放世界常规模式。Exbound（外切）：整张地图始终留在可见地面内 —— 用于把地图完整框进屏幕。")]
        public CameraBoundType boundType = CameraBoundType.Inbound;
        [Tooltip("填了这一项，就忽略 mapMinXZ / mapMaxXZ，改由地块流式加载组件推导边界（gridOriginXZ + gridSize * blockSize）。")]
        public WorldChunkStreaming boundsSource;
        [Tooltip("地图矩形的最小角（XZ）。")]
        public Vector2 mapMinXZ = new Vector2(0f, 0f);
        [Tooltip("地图矩形的最大角（XZ）。")]
        public Vector2 mapMaxXZ = new Vector2(1000f, 1000f);
        [Tooltip("可活动区域相对地图矩形四边各向外扩张多少米 —— 相机的投射矩形因此允许越出地图边缘一点，地形边界不会被死死钉在屏幕边上。这里填的是缩放值最小（相机最低）时的外扩量；实际外扩量按相机高度成比例放大，默认高度曲线是线性的，也就是随缩放值线性增长。")]
        public float boundsPadding = 20f;

        [Header("Gizmos")]
        [Tooltip("绘制地图矩形和相机的地面覆盖范围。Scene 视图始终可见；运行时在 Game 视图里需要打开 Gizmos 开关。")]
        public bool drawGizmos = true;
        [Tooltip("除了外接矩形，把视锥投在地面上的原始梯形也一起描出来。真正限制相机移动的是那个矩形。")]
        public bool drawFrustumTrapezoid = true;

        /// <summary>视口四角，按环绕顺序：左下、右下、右上、左上。</summary>
        static readonly Vector3[] ViewportCorners =
        {
            new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f),
            new Vector3(1f, 1f, 0f), new Vector3(0f, 1f, 0f),
        };

        // ------------------------------------------------------------------------ 对外接口

        /// <summary>归一化缩放值发生变化时触发。</summary>
        public event Action<float> onZoomValueChange;
        /// <summary>屏幕中心对应的地面点发生变化时触发。</summary>
        public event Action<Vector3> onViewCenterChange;

        /// <summary>归一化缩放值：0 = 最近（高度曲线在 0 处的取值），1 = 最远（曲线在 1 处的取值）。</summary>
        public float ZoomValue => _zoom;

        /// <summary>屏幕中心对应的地面点 —— 相机的逻辑位置。</summary>
        public Vector3 ViewCenter => _lookAt;

        /// <summary>可见地面覆盖范围的纵向深度（世界单位）。用来把平移速度表示成"每秒多少屏"，
        /// 这样在任意高度下读数含义都一致。</summary>
        public float ViewPortHeight { get; private set; }

        /// <summary>当前相机相对地面的高度（米），即高度曲线在当前缩放值处的取值。</summary>
        public float CurrentHeight => EvaluateHeight();

        /// <summary>兼容旧调用方而保留；与 <see cref="ZoomValue"/> 完全相同。</summary>
        public float CurrentHeight01 => _zoom;

        /// <summary>把视野中心直接挪到某个地面位置。会取消正在进行的惯性滑行。</summary>
        public void MoveTo(Vector3 groundPos)
        {
            _lookAt = new Vector3(groundPos.x, groundY, groundPos.z);
            _zoomAnchorActive = false;
            CancelInertia();
        }

        /// <summary>设置目标缩放值（0 = 最近，1 = 最远）。immediate 为 true 则跳过平滑直接到位。</summary>
        public void ZoomTo(float zoom01, bool immediate = false)
        {
            _targetZoom = Mathf.Clamp01(zoom01);
            _zoomAnchorActive = false;
            if (immediate)
            {
                _zoom = _targetZoom;
                _zoomVel = 0f;
                _footprintDirty = true;
            }
        }

        /// <summary>
        /// 相机投在地面上的梯形，按视口环绕顺序填入（左下、右下、右上、左上）。
        /// 当视野完全不与地面相交时返回 false —— 见 <see cref="ProjectFrustumToGround"/>。
        /// 之所以对外暴露，是为了让地块流式加载直接复用相机自己的覆盖范围，
        /// 而不是另算一份细微不同的。
        /// </summary>
        public bool TryGetGroundFootprint(Vector3[] quad4)
        {
            if (quad4 == null || quad4.Length < 4) return false;
            return ProjectFrustumToGround(quad4);
        }

        /// <summary>可见地面覆盖范围的 XZ 外接矩形（世界空间）。</summary>
        public bool TryGetFootprintRect(out Rect rect)
        {
            rect = default;
            if (!ProjectFrustumToGround(_groundQuad)) return false;
            GroundQuadBounds(_groundQuad, out Vector2 min, out Vector2 max);
            rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return true;
        }

        // ------------------------------------------------------------------------ 内部状态

        Camera _cam;                        // 本组件所在的 Camera
        Vector3 _lookAt;                    // 地面注视点，也就是相机的权威状态
        float _zoom;                        // 当前（已平滑）缩放值
        float _targetZoom;                  // 目标缩放值，平滑向它逼近
        float _zoomVel;                     // SmoothDamp 的速度累加器
        float _appliedPitch = float.NaN;    // 已写入 Transform 的俯角，用于跳过重复赋值
        float _appliedYaw = float.NaN;      // 已写入 Transform 的朝向，同上
        float _lastNotifiedZoom = float.NaN;                        // 上次派发过事件的缩放值
        Vector3 _lastNotifiedCenter = new Vector3(float.NaN, 0f, 0f); // 上次派发过事件的注视点

        // 拖动
        bool _dragArmed;            // 按键已按下，但还没越过拖动阈值
        bool _dragging;             // 已确认进入拖动
        Vector2 _pressScreen;       // 按下时的屏幕坐标，用来判定阈值
        Vector3 _grabWorld;         // 被"抓住"的地面点，拖动过程中要一直贴在光标下
        Vector3 _lastPanDir;        // 最后一次平移的方向，松手后作为滑行方向

        // 惯性
        bool _gliding;              // 是否正在滑行
        Vector3 _glideDir;          // 滑行方向（已归一化）
        float _glideSpeed;          // 滑行初速度（米/秒）
        float _glideStartTime;      // 滑行起始时刻

        // 朝光标缩放用的锚点。整段平滑过程都要保持，而不只是滚轮那一帧，
        // 否则缩放收敛期间画面会慢慢漂回屏幕中心。
        bool _zoomAnchorActive;     // 锚点是否有效
        Vector2 _zoomAnchorScreen;  // 锚点所在的屏幕像素
        Vector3 _zoomAnchorWorld;   // 锚点对应的世界坐标，要一直钉在上面那个像素下

        // 缓存的地面覆盖范围。这些偏移只取决于旋转、视场角、宽高比和高度，与 XZ 无关，
        // 所以平移时可以原样沿用，只有画面本身变化时才需要重算。
        // 参考工程的相机也是这么做的（边界随缩放刷新，而不是每帧刷新）。
        readonly Vector3[] _groundQuad = new Vector3[4]; // 视锥投在地面上的梯形四个角
        bool _footprintDirty = true;    // 缓存是否已失效
        bool _footprintValid;           // 上次投影是否成功（视野是否与地面相交）
        Vector2 _footLo, _footHi;       // 外接 AABB 相对相机 XZ 的偏移
        Vector2 _innerLo, _innerHi;     // 梯形内最大的轴对齐内接矩形，同样是相对偏移
        float _fpHeight, _fpFov, _fpPitch, _fpYaw, _fpAspect;   // 缓存对应的画面参数，用作失效判据

        readonly List<PositionTrace> _trace = new List<PositionTrace>(); // 拖动轨迹采样，用于算松手速度

        /// <summary>一条拖动轨迹采样：某个时刻抓到的地面点。</summary>
        struct PositionTrace
        {
            public Vector3 pos;
            public float time;
        }

        const float kTraceWindow = 0.1f;    // 松手速度在这么多秒的窗口内取平均

        // ------------------------------------------------------------------------ 生命周期

        void Awake()
        {
            _cam = GetComponent<Camera>();
            if (zoomCurve == null) zoomCurve = new CameraZoomCurve();

            // 先定旋转：下面所有推导都依赖相机朝向。
            ApplyLook();

            // 场景里摆好的相机高度不一定正好落在曲线上，所以反查出最接近它的缩放值，
            // 再用曲线在该处的取值作为实际起始高度 —— 这样高度永远由曲线说话。
            _zoom = _targetZoom = zoomCurve.FindZoomForHeight(transform.position.y - groundY);
            float height = EvaluateHeight();
            _lookAt = LookAtFromPosition(new Vector3(transform.position.x, groundY + height, transform.position.z));

            ApplyFrame();
        }

        void Update()
        {
            StepZoom();
            HandleZoomInput();
            HandleDrag();
            HandleInertia();
            ApplyFrame();
        }

        /// <summary>
        /// 推进平滑后的缩放值。刻意与 <see cref="ApplyFrame"/> 分开：SmoothDamp 内部会吃掉
        /// Time.deltaTime，如果把它塞进画面重建里，那么在同一帧里又拖动又滑行时，
        /// 缩放会以两三倍速度收敛。拆开之后 ApplyFrame 是幂等的，想调多少次都安全。
        /// </summary>
        void StepZoom()
        {
            if (Mathf.Approximately(_zoom, _targetZoom))
            {
                _zoom = _targetZoom;
                _zoomAnchorActive = false;
                return;
            }

            if (heightSmoothTime > 0f)
                _zoom = Mathf.SmoothDamp(_zoom, _targetZoom, ref _zoomVel, heightSmoothTime);
            else
                _zoom = _targetZoom;
            _footprintDirty = true;
        }

        /// <summary>当前缩放值对应的相机高度（米）。高度曲线是唯一来源。</summary>
        float EvaluateHeight()
        {
            return zoomCurve != null ? zoomCurve.EvaluateHeight(_zoom) : kFallbackHeight;
        }

        /// <summary>
        /// 由 <see cref="_lookAt"/> 和 <see cref="_zoom"/> 重建 Transform，然后做边界约束。
        /// 顺序很关键：必须先定视角才能量出地面覆盖范围，
        /// 而必须先知道覆盖范围才能约束位置。
        /// </summary>
        void ApplyFrame()
        {
            ApplyLook();

            float height = EvaluateHeight();
            transform.position = PositionFromLookAt(_lookAt, height);
            RefreshFootprint(height);

            if (_zoomAnchorActive)
            {
                // 平移视野中心，把被锚住的地面点送回它原来那个像素下面。
                // 事后修正只需要一次射线检测，而且对任意俯角 / 视场角组合都精确，
                // 比事先解析求偏移量稳得多。
                if (RaycastGround(_zoomAnchorScreen, out Vector3 anchorNow))
                {
                    Vector3 shift = _zoomAnchorWorld - anchorNow;
                    shift.y = 0f;
                    if (shift.sqrMagnitude > 1e-8f)
                    {
                        _lookAt += shift;
                        transform.position = PositionFromLookAt(_lookAt, height);
                    }
                }
                else
                {
                    _zoomAnchorActive = false;
                }
            }

            if (clampToMapBounds)
            {
                // 最多迭代几轮。在精确算术下覆盖范围偏移是平移不变的，一轮就该够 —— 但这些偏移
                // 来自*约束前*位置上构造的射线方向，而 MoveTo 从地图外很远处过来时，射线是在很大的
                // 坐标上建的，浮点误差再乘上一千多米的射线长度，就有几十厘米的量级。每次重新测量都
                // 比上一次更靠近地图，残差因此按几何级数收缩。正常帧上没有额外开销：第一轮压根不会
                // 移动，直接跳出。
                for (int pass = 0; pass < 3; pass++)
                {
                    Vector3 clamped = ClampByFootprint(transform.position);
                    if ((clamped - transform.position).sqrMagnitude <= 0f) break;
                    transform.position = clamped;
                    // 逻辑状态必须跟着 Transform 一起改，否则下一帧又会重建出同一个越界位置，
                    // 与约束永远互相顶着。
                    _lookAt = LookAtFromPosition(clamped);
                    _footprintDirty = true;
                    RefreshFootprint(height);
                }
            }

            Notify();
        }

        /// <summary>缩放值或注视点确实变了才派发事件，避免每帧空转回调。</summary>
        void Notify()
        {
            if (!Mathf.Approximately(_zoom, _lastNotifiedZoom))
            {
                _lastNotifiedZoom = _zoom;
                onZoomValueChange?.Invoke(_zoom);
            }
            if ((_lookAt - _lastNotifiedCenter).sqrMagnitude > 1e-6f)
            {
                _lastNotifiedCenter = _lookAt;
                onViewCenterChange?.Invoke(_lookAt);
            }
        }

        // ------------------------------------------------------------------------ 视角

        /// <summary>
        /// 写入相机旋转。俯角和朝向都是死的配置值，不受缩放影响 ——
        /// 这正是"拖动和滚轮不得改变 45° 俯视角"这条要求落地的地方。
        /// 只有面板上真的改了角度才会去动 Transform。
        /// </summary>
        void ApplyLook()
        {
            if (pitchDegrees == _appliedPitch && yawDegrees == _appliedYaw) return;
            _appliedPitch = pitchDegrees;
            _appliedYaw = yawDegrees;
            transform.rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
            _footprintDirty = true;
        }

        // ------------------------------------------------------------------------ 输入

        /// <summary>处理滚轮缩放，并在需要时记下"朝光标缩放"的锚点。</summary>
        void HandleZoomInput()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) <= 0.0001f) return;
            if (zoomCurve == null) return;

            // 用高度曲线的取值跨度把"每格多少米"换算成归一化缩放值上的步长，
            // 这样改曲线的高低范围时，scrollSensitivity 的手感不会跟着走样。
            zoomCurve.GetHeightRange(out float minHeight, out float maxHeight);
            float range = maxHeight - minHeight;
            if (range < 1e-4f) return;

            // 向上滚（正值）是拉近，也就是让"拉远程度"变小。
            float step = scroll * scrollSensitivity / range;
            float next = Mathf.Clamp01(_targetZoom - step);
            if (Mathf.Approximately(next, _targetZoom)) return;
            _targetZoom = next;

            if (zoomTowardCursor && RaycastGround(Input.mousePosition, out Vector3 anchor))
            {
                _zoomAnchorScreen = Input.mousePosition;
                _zoomAnchorWorld = anchor;
                _zoomAnchorActive = true;
            }
        }

        /// <summary>处理按住拖动地面的平移：越过阈值才算平移，过程中把抓住的地面点钉在光标下。</summary>
        void HandleDrag()
        {
            if (Input.GetMouseButtonDown(dragMouseButton))
            {
                _pressScreen = Input.mousePosition;
                _dragArmed = RaycastGround(Input.mousePosition, out _grabWorld);
                _dragging = false;
                _trace.Clear();
            }

            if (!Input.GetMouseButton(dragMouseButton))
            {
                if (_dragging) EndDrag();
                _dragArmed = false;
                _dragging = false;
                return;
            }

            if (_dragArmed && !_dragging)
            {
                // 只有越过阈值，这次按下才升级成平移；在阈值以内都算点击。
                Vector2 moved = (Vector2)Input.mousePosition - _pressScreen;
                if (moved.sqrMagnitude < dragThresholdPixels * dragThresholdPixels) return;
                _dragging = true;
                _zoomAnchorActive = false;   // 拖动接管，缩放锚点让位
                CancelInertia();
                if (RaycastGround(Input.mousePosition, out Vector3 grab))
                {
                    _grabWorld = grab;
                    _trace.Add(new PositionTrace { pos = grab, time = Time.time });
                }
            }

            if (!_dragging) return;

            // 用*当前*这一帧重新求出光标下的世界点，然后平移视野中心，
            // 让最初抓住的那个点回到光标下面。
            if (!RaycastGround(Input.mousePosition, out Vector3 current)) return;

            Vector3 delta = _grabWorld - current;
            delta.y = 0f;
            if (delta.sqrMagnitude > 0f)
            {
                _lastPanDir = delta;
                _lookAt += delta;
                // 约束发生在 ApplyFrame 里；约束之后重新取一次抓取点，是让被挡住的平移
                // 表现成一堵软墙，而不是把没生效的位移累积成抖动。
                ApplyFrame();
                if (RaycastGround(Input.mousePosition, out Vector3 reGrab)) _grabWorld = reGrab;
            }

            RecordTrace(current);
        }

        /// <summary>记一条拖动轨迹采样，并丢掉过老的采样，避免长时间慢拖把列表撑大。</summary>
        void RecordTrace(Vector3 groundPos)
        {
            float now = Time.time;
            _trace.Add(new PositionTrace { pos = groundPos, time = now });
            // 算松手速度只用得到尾巴那一小段，更老的直接丢弃。
            float cutoff = now - kTraceWindow * 2.5f;
            int drop = 0;
            while (drop < _trace.Count - 1 && _trace[drop].time < cutoff) drop++;
            if (drop > 0) _trace.RemoveRange(0, drop);
        }

        /// <summary>松手：按最近一小段轨迹的平均速度决定是否进入惯性滑行。</summary>
        void EndDrag()
        {
            if (!inertiaEnabled || _trace.Count < 2 || ViewPortHeight <= 0f)
            {
                _trace.Clear();
                return;
            }

            // 从最新一条采样往回走，累计最近 kTraceWindow 秒内走过的地面总路程。
            // 累加路径长度（而不是首尾直线距离）意味着划弧线的甩动也能保留真实速度。
            Vector3 pos = _trace[_trace.Count - 1].pos;
            float stamp = Time.time - kTraceWindow;
            float distance = 0f;
            float earliest = _trace[_trace.Count - 1].time;
            for (int i = _trace.Count - 1; i >= 0; i--)
            {
                if (_trace[i].time < stamp) break;
                distance += Vector3.Distance(_trace[i].pos, pos);
                pos = _trace[i].pos;
                earliest = _trace[i].time;
            }
            _trace.Clear();

            float elapsed = Time.time - earliest;
            if (elapsed <= 1e-4f || distance <= 0f) return;

            float screenSpeed = distance / elapsed / ViewPortHeight;
            if (screenSpeed < inertiaMinScreenSpeed) return;
            screenSpeed = Mathf.Min(screenSpeed, inertiaMaxScreenSpeed);

            _glideDir = _lastPanDir;
            _glideDir.y = 0f;
            if (_glideDir.sqrMagnitude < 1e-8f) return;
            _glideDir.Normalize();
            _glideSpeed = screenSpeed * ViewPortHeight;
            _glideStartTime = Time.time;
            _gliding = true;
        }

        /// <summary>推进惯性滑行；撞上地图边界推不动了就提前结束。</summary>
        void HandleInertia()
        {
            if (!_gliding) return;
            if (_dragging || inertiaDuration <= 0f) { CancelInertia(); return; }

            float t = (Time.time - _glideStartTime) / inertiaDuration;
            if (t >= 1f) { CancelInertia(); return; }

            float decay = inertiaDecay != null && inertiaDecay.length > 0 ? inertiaDecay.Evaluate(t) : 1f - t;
            Vector3 step = _glideDir * (_glideSpeed * Mathf.Max(0f, decay) * Time.deltaTime);
            if (step.sqrMagnitude < 1e-10f) { CancelInertia(); return; }

            Vector3 before = _lookAt;
            _lookAt += step;
            ApplyFrame();
            // 已经顶到地图边缘、这一步基本没挪动 —— 直接结束滑行，
            // 而不是在墙上磨完剩下的时长。
            if ((_lookAt - before).sqrMagnitude < step.sqrMagnitude * 0.01f) CancelInertia();
        }

        /// <summary>立即停止惯性滑行。</summary>
        void CancelInertia()
        {
            _gliding = false;
            _glideSpeed = 0f;
        }

        // ------------------------------------------------------------------------ 几何计算

        /// <summary>相机位于 <paramref name="pos"/> 时会注视的地面点。</summary>
        Vector3 LookAtFromPosition(Vector3 pos)
        {
            Vector3 fwd = transform.forward;
            if (fwd.y > -1e-4f) return new Vector3(pos.x, groundY, pos.z);
            float d = (pos.y - groundY) / -fwd.y;
            Vector3 p = pos + fwd * d;
            p.y = groundY;
            return p;
        }

        /// <summary>
        /// <see cref="LookAtFromPosition"/> 的逆运算：沿视线方向后退足够距离，使相机既高出地面
        /// <paramref name="height"/> 米，又仍然注视着 <paramref name="lookAt"/>。
        /// 等价于参考工程相机里的 height/sin(pitch) 距离，但这里是从 forward 向量推的，
        /// 因此对任意水平朝向都成立。
        /// </summary>
        Vector3 PositionFromLookAt(Vector3 lookAt, float height)
        {
            Vector3 fwd = transform.forward;
            if (fwd.y > -1e-4f) return new Vector3(lookAt.x, groundY + height, lookAt.z);
            Vector3 p = lookAt - fwd * (height / -fwd.y);
            p.y = groundY + height;
            return p;
        }

        /// <summary>把屏幕坐标打到地面平面上，取交点。射线打不到地面时返回 false。</summary>
        bool RaycastGround(Vector2 screenPos, out Vector3 worldPoint)
        {
            worldPoint = default;
            var cam = _cam != null ? _cam : Camera.main;
            if (cam == null) return false;
            Ray ray = cam.ScreenPointToRay(screenPos);
            if (Mathf.Abs(ray.direction.y) < 1e-6f) return false;
            float t = (groundY - ray.origin.y) / ray.direction.y;
            if (t <= 0f) return false;
            worldPoint = ray.origin + ray.direction * t;
            return true;
        }

        /// <summary>
        /// 把视锥四条角射线投到地面平面上，按视口环绕顺序填入 <paramref name="result"/> ——
        /// 也就是相机实际看到的那个梯形。求交点时<i>精确</i>计算，刻意忽略 <c>farClipPlane</c>：
        /// 把角点截断在远裁剪面上会得到一个悬在空中的点，其 XZ 比真正的地面交点更靠内，
        /// 于是覆盖范围被低估，相机就能一路平移到画面滑出地图。
        ///
        /// 当某条角射线与地平线齐平或朝上时返回 false —— 那种情况下它压根不与地面相交，
        /// 覆盖范围是无界的。
        /// </summary>
        bool ProjectFrustumToGround(Vector3[] result)
        {
            var cam = _cam != null ? _cam : GetComponent<Camera>();
            if (cam == null) return false;
            for (int i = 0; i < 4; i++)
            {
                Ray ray = cam.ViewportPointToRay(ViewportCorners[i]);
                if (Mathf.Abs(ray.direction.y) < 1e-6f) return false;   // 与地平线齐平
                float t = (groundY - ray.origin.y) / ray.direction.y;
                if (t <= 0f) return false;                              // 朝地平线以上
                result[i] = ray.origin + ray.direction * t;
            }
            return true;
        }

        /// <summary>
        /// 重算缓存的覆盖范围偏移，但只在决定它们的那些画面参数真的变了时才动手。
        /// 单纯平移永远不会让缓存失效，这正是关键所在：一次拖动完全不需要做射线运算。
        /// </summary>
        void RefreshFootprint(float height)
        {
            float fov = _cam != null ? _cam.fieldOfView : 60f;
            float aspect = _cam != null ? _cam.aspect : 1.777f;
            if (!_footprintDirty &&
                height == _fpHeight && fov == _fpFov && aspect == _fpAspect &&
                _appliedPitch == _fpPitch && _appliedYaw == _fpYaw)
                return;

            _fpHeight = height; _fpFov = fov; _fpAspect = aspect;
            _fpPitch = _appliedPitch; _fpYaw = _appliedYaw;
            _footprintDirty = false;

            _footprintValid = ProjectFrustumToGround(_groundQuad);
            if (!_footprintValid)
            {
                ViewPortHeight = 0f;
                return;
            }

            Vector2 camXZ = new Vector2(transform.position.x, transform.position.z);
            GroundQuadBounds(_groundQuad, out Vector2 qMin, out Vector2 qMax);
            _footLo = qMin - camXZ;
            _footHi = qMax - camXZ;
            ViewPortHeight = qMax.y - qMin.y;

            InscribedQuadBounds(_groundQuad, out Vector2 iMin, out Vector2 iMax);
            _innerLo = iMin - camXZ;
            _innerHi = iMax - camXZ;
        }

        /// <summary>
        /// 按 <see cref="boundType"/> 约束 <paramref name="pos"/>，使相机的地面覆盖范围符合地图矩形。
        ///
        /// <b>Inbound</b> 约束的是覆盖范围的<i>外接</i>轴对齐矩形，要求它留在地图内。覆盖范围本身是个
        /// 梯形（近边窄、远边宽），取外接矩形是保守做法 —— 这样在任何平移位置上，可见梯形的任何一角
        /// 都不会离开地形。<b>Exbound</b> 则完全对称：梯形内最大的<i>内接</i>矩形必须包住地图。
        ///
        /// 两者都基于相对当前相机 XZ 测出的偏移量。这是精确的，因为旋转是固定的，梯形在 XZ 上
        /// 平移不变 —— 同一套偏移对任何候选位置都成立。
        /// </summary>
        Vector3 ClampByFootprint(Vector3 pos)
        {
            GetBounds(out Vector2 min, out Vector2 max);
            if (!_footprintValid)
            {
                pos.x = Mathf.Clamp(pos.x, min.x, max.x);
                pos.z = Mathf.Clamp(pos.z, min.y, max.y);
                return pos;
            }

            if (boundType == CameraBoundType.Exbound)
            {
                pos.x = ClampAxis(pos.x, max.x - _innerHi.x, min.x - _innerLo.x, min.x, max.x, _innerLo.x, _innerHi.x);
                pos.z = ClampAxis(pos.z, max.y - _innerHi.y, min.y - _innerLo.y, min.y, max.y, _innerLo.y, _innerHi.y);
            }
            else
            {
                pos.x = ClampAxis(pos.x, min.x - _footLo.x, max.x - _footHi.x, min.x, max.x, _footLo.x, _footHi.x);
                pos.z = ClampAxis(pos.z, min.y - _footLo.y, max.y - _footHi.y, min.y, max.y, _footLo.y, _footHi.y);
            }
            return pos;
        }

        /// <summary>
        /// 约束到 [lo, hi]。当这个窗口为空时 —— Inbound 下覆盖范围比地图还宽，或 Exbound 下比地图还窄 ——
        /// 没有任何位置能满足约束，此时把覆盖范围居中对齐到地图上，让差额均摊到两边，
        /// 而不是一头贴死在某一侧。
        /// </summary>
        static float ClampAxis(float v, float lo, float hi, float mapMin, float mapMax, float offLo, float offHi)
        {
            if (lo <= hi) return Mathf.Clamp(v, lo, hi);
            return 0.5f * (mapMin + mapMax - offLo - offHi);
        }

        /// <summary>求地面梯形的 XZ 外接矩形。</summary>
        static void GroundQuadBounds(Vector3[] quad, out Vector2 min, out Vector2 max)
        {
            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector3 h = quad[i];
                if (h.x < minX) minX = h.x;
                if (h.x > maxX) maxX = h.x;
                if (h.z < minZ) minZ = h.z;
                if (h.z > maxZ) maxZ = h.z;
            }
            min = new Vector2(minX, minZ);
            max = new Vector2(maxX, maxZ);
        }

        /// <summary>
        /// 地面梯形的轴对齐内接矩形，取每个轴上的次小值和次大值。对固定俯角产生的那种梯形来说 ——
        /// 两条边平行于 X 轴、宽度随 Z 单调增加 —— 这恰好就是"近边宽度 × 完整深度"，
        /// 并且确实被包含在四边形内。只有当水平朝向把四边形转离坐标轴时它才退化成近似，
        /// 而本相机不会那样做。
        /// </summary>
        static void InscribedQuadBounds(Vector3[] quad, out Vector2 min, out Vector2 max)
        {
            min = new Vector2(SecondSmallest(quad[0].x, quad[1].x, quad[2].x, quad[3].x),
                              SecondSmallest(quad[0].z, quad[1].z, quad[2].z, quad[3].z));
            max = new Vector2(SecondLargest(quad[0].x, quad[1].x, quad[2].x, quad[3].x),
                              SecondLargest(quad[0].z, quad[1].z, quad[2].z, quad[3].z));
            if (max.x < min.x) max.x = min.x;
            if (max.y < min.y) max.y = min.y;
        }

        /// <summary>四个数里的次小值。</summary>
        static float SecondSmallest(float a, float b, float c, float d)
        {
            float m1 = Mathf.Min(Mathf.Min(a, b), Mathf.Min(c, d));
            float m2 = float.MaxValue;
            bool skipped = false;
            AccumulateSecond(a, m1, ref m2, ref skipped, true);
            AccumulateSecond(b, m1, ref m2, ref skipped, true);
            AccumulateSecond(c, m1, ref m2, ref skipped, true);
            AccumulateSecond(d, m1, ref m2, ref skipped, true);
            return m2 == float.MaxValue ? m1 : m2;
        }

        /// <summary>四个数里的次大值。</summary>
        static float SecondLargest(float a, float b, float c, float d)
        {
            float m1 = Mathf.Max(Mathf.Max(a, b), Mathf.Max(c, d));
            float m2 = float.MinValue;
            bool skipped = false;
            AccumulateSecond(a, m1, ref m2, ref skipped, false);
            AccumulateSecond(b, m1, ref m2, ref skipped, false);
            AccumulateSecond(c, m1, ref m2, ref skipped, false);
            AccumulateSecond(d, m1, ref m2, ref skipped, false);
            return m2 == float.MinValue ? m1 : m2;
        }

        /// <summary>把一个值折进"第二名"里，其中只跳过极值的一份拷贝，
        /// 这样极值重复出现时（轴对齐的覆盖范围）也能正确把自己报出来。</summary>
        static void AccumulateSecond(float v, float extreme, ref float runnerUp, ref bool skipped, bool wantMin)
        {
            if (!skipped && v == extreme) { skipped = true; return; }
            if (wantMin) { if (v < runnerUp) runnerUp = v; }
            else { if (v > runnerUp) runnerUp = v; }
        }

        /// <summary>
        /// 当前缩放值下的边界外扩量（米）。<see cref="boundsPadding"/> 是缩放值最小（相机最低）时的
        /// 取值，之后按相机高度成比例放大；高度曲线默认是线性的，所以它就是随缩放值线性增长。
        ///
        /// 挂在高度上而不是写死一个斜率，是因为外扩量的含义是"相对当前视野尺度留多少余量"：
        /// 拉得越远，地面覆盖范围越大，同样的 20 米就越看不出来，余量必须跟着一起长。
        /// 附带的好处是重新调整高度曲线时这里不用改。
        /// </summary>
        float EvaluatePadding()
        {
            if (boundsPadding == 0f || zoomCurve == null) return boundsPadding;
            // EvaluateHeight 内部已经把高度夹在 1 米以上，所以这里不会除到 0。
            return boundsPadding * (EvaluateHeight() / zoomCurve.EvaluateHeight(0f));
        }

        /// <summary>取地图矩形（优先来自流式加载组件），并套上随缩放增长的四边外扩量。</summary>
        void GetBounds(out Vector2 min, out Vector2 max)
        {
            if (boundsSource != null)
            {
                min = boundsSource.gridOriginXZ;
                float side = boundsSource.gridSize * boundsSource.blockSize;
                max = new Vector2(min.x + side, min.y + side);
            }
            else
            {
                min = mapMinXZ;
                max = mapMaxXZ;
            }

            // 向外扩张：可活动区域是"地图大小 + 外扩量"，也就是允许相机的投射矩形越出地图边缘一些。
            float pad = EvaluatePadding();
            min -= new Vector2(pad, pad);
            max += new Vector2(pad, pad);
            // 只有把外扩量填成负数（当成内收用）才会走到这里。
            if (max.x < min.x) max.x = min.x;
            if (max.y < min.y) max.y = min.y;
        }

#if UNITY_EDITOR
        /// <summary>面板改动后让覆盖范围缓存失效，好让 Gizmos 立刻反映新参数。</summary>
        void OnValidate()
        {
            _footprintDirty = true;
        }

        // 用 OnDrawGizmos 而不是 OnDrawGizmosSelected，这样运行时不必一直在层级面板里选中相机
        // 也能看到覆盖范围。在 Game 视图里需要把 Gizmos 开关打开。
        void OnDrawGizmos()
        {
            if (!drawGizmos) return;

            if (clampToMapBounds)
            {
                GetBounds(out Vector2 min, out Vector2 max);
                Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.9f);
                DrawGroundRect(min, max, groundY + 0.1f);
            }

            // 几条轮廓线之间在 Y 上稍微错开，避免互相 z-fighting。
            if (!ProjectFrustumToGround(_groundQuad)) return;

            if (drawFrustumTrapezoid)
            {
                Gizmos.color = new Color(1f, 0.95f, 0.2f, 0.55f);
                for (int i = 0; i < 4; i++)
                {
                    Vector3 a = _groundQuad[i];
                    Vector3 b = _groundQuad[(i + 1) % 4];
                    a.y += 0.2f; b.y += 0.2f;
                    Gizmos.DrawLine(a, b);
                }
            }

            // 约束真正保证留在地图上的那个矩形：Inbound 是梯形的外接框，Exbound 是内接框。
            if (boundType == CameraBoundType.Exbound)
            {
                InscribedQuadBounds(_groundQuad, out Vector2 iMin, out Vector2 iMax);
                Gizmos.color = new Color(1f, 0.35f, 0.8f, 0.95f);
                DrawGroundRect(iMin, iMax, groundY + 0.3f);
            }
            else
            {
                GroundQuadBounds(_groundQuad, out Vector2 qMin, out Vector2 qMax);
                Gizmos.color = new Color(0.2f, 1f, 0.9f, 0.95f);
                DrawGroundRect(qMin, qMax, groundY + 0.3f);
            }
        }

        /// <summary>在给定高度上画一个 XZ 矩形轮廓。</summary>
        static void DrawGroundRect(Vector2 min, Vector2 max, float y)
        {
            Vector3 a = new Vector3(min.x, y, min.y);
            Vector3 b = new Vector3(max.x, y, min.y);
            Vector3 c = new Vector3(max.x, y, max.y);
            Vector3 d = new Vector3(min.x, y, max.y);
            Gizmos.DrawLine(a, b); Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, d); Gizmos.DrawLine(d, a);
        }
#endif
    }
}
