using System.Collections.Generic;
using Framework;
using GameEntry;
using Google.Protobuf.WellKnownTypes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GameMain
{
    /// <summary>
    /// Main 场景中的本地玩家：点击地面发起寻路，沿服务端返回的 corners 本地移动，
    /// 用 MoveSync 校正位置；相机通过 WorldCameraController 跟随。
    /// </summary>
    public sealed class LocalPlayerController : MonoBehaviour
    {
        private const float kVisualYOffset = 1f;
        private const float kSyncCorrectionThreshold = 0.75f;
        private static readonly Color kMarchLineColor = new Color(0.2f, 1f, 0.95f, 1f);

        /// <summary>行军线预制体的 Addressables 地址。</summary>
        private const string kMarchLineAddress = "Assets/AssetBundles/Prefab/MarchLine/TroopMarchLineWorld.prefab";

        private readonly List<Vector3> _corners = new List<Vector3>();

        private ulong _playerId;
        private Vector3 _spawn;
        private uint _pathId;
        private uint _seq;
        private float _speed = 4f;
        private bool _moving;
        private int _cornerIndex;
        private Vector3 _serverPosition;
        private MarchLine _marchLine;
        private bool _marchLineLoading;
        private bool _destroyed;
        private readonly List<Vector3> _marchPath = new List<Vector3>();

        public void Initialize(ulong playerId, Vector3 spawn)
        {
            _playerId = playerId;
            _spawn = spawn;
            transform.position = new Vector3(spawn.x, spawn.y + kVisualYOffset, spawn.z);
            transform.rotation = Quaternion.identity;
        }

        /// <summary>服务端确认后的移动速度；正在移动时立即生效。</summary>
        public void SetMoveSpeed(float speed)
        {
            _speed = Mathf.Clamp(speed, 1f, 20f);
        }

        private void Update()
        {
            HandleClick();
            UpdateLocalMovement();
        }

        private void HandleClick()
        {
            if (!Input.GetMouseButtonDown(0))
                return;

            if (GateInfoPanel.IsOpen)
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            var camera = Camera.main;
            if (camera == null)
                return;

            var ray = camera.ScreenPointToRay(Input.mousePosition);
            Vector3 target;

            if (Physics.Raycast(ray, out var hit, 1000f))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    return;

                var gate = hit.collider != null
                    ? hit.collider.GetComponentInParent<GateBuilding>()
                    : null;
                if (gate != null)
                {
                    PathfindingMgr.I?.RequestGateInfo(gate.InstanceId);
                    return;
                }

                target = hit.point;
            }
            else
            {
                // 地形块还没流式加载时没有碰撞体，用玩家出生高度的地面平面兜底
                var plane = new Plane(Vector3.up, new Vector3(0f, _spawn.y, 0f));
                if (!plane.Raycast(ray, out var enter))
                    return;
                target = ray.GetPoint(enter);
            }

            _seq++;
            _marchLine?.SetActive(false);
            FW.NetMgr.SendMsg(new PathFindReq
            {
                Seq = _seq,
                Target = ToVec3(target),
                ClientPos = ToVec3(transform.position - Vector3.up * kVisualYOffset)
            });

            Debug.Log($"[Path] 发送寻路请求 seq={_seq} target={target}");
        }

        public void OnPathFindAck(PathFindAck ack)
        {
            if (ack.Seq != _seq)
                return;

            if (ack.Code != 0)
            {
                Debug.LogWarning($"[Path] 寻路失败 code={ack.Code} message={ack.Message}");
                return;
            }

            _pathId = ack.PathId;
            _speed = ack.Speed > 0f ? ack.Speed : 4f;
            _corners.Clear();

            foreach (var corner in ack.Corners)
                _corners.Add(new Vector3(corner.X, corner.Y, corner.Z));

            if (ack.Start != null)
            {
                transform.position = new Vector3(
                    ack.Start.X,
                    ack.Start.Y + kVisualYOffset,
                    ack.Start.Z);
            }

            _cornerIndex = _corners.Count > 1 ? 1 : _corners.Count;
            _moving = _cornerIndex < _corners.Count;

            if (_moving)
            {
                EnsureMarchLine();
                RefreshMarchLine();
            }
            else
            {
                _marchLine?.SetActive(false);
            }

            Debug.Log($"[Path] 收到路径 pathId={ack.PathId} corners={_corners.Count} speed={_speed}");
        }

        public void OnMoveSync(MoveSync sync)
        {
            if (sync.PlayerId != _playerId || sync.Position == null)
                return;

            _serverPosition = new Vector3(sync.Position.X, sync.Position.Y, sync.Position.Z);

            if (!sync.Moving && sync.PathId == _pathId)
            {
                _moving = false;
                _cornerIndex = _corners.Count;
                _marchLine?.SetActive(false);
                transform.position = _serverPosition + Vector3.up * kVisualYOffset;
                transform.rotation = Quaternion.Euler(0f, sync.RotationY, 0f);
                return;
            }

            if (sync.PathId == _pathId)
            {
                var localFeet = transform.position - Vector3.up * kVisualYOffset;
                if (Vector3.Distance(Flat(localFeet), Flat(_serverPosition)) > kSyncCorrectionThreshold)
                {
                    transform.position = Vector3.Lerp(
                        transform.position,
                        _serverPosition + Vector3.up * kVisualYOffset,
                        0.35f);
                }
            }
        }

        private void UpdateLocalMovement()
        {
            if (!_moving)
                return;

            if (_cornerIndex >= _corners.Count)
            {
                _moving = false;
                _marchLine?.SetActive(false);
                return;
            }

            var target = _corners[_cornerIndex] + Vector3.up * kVisualYOffset;
            var current = transform.position;
            var delta = target - current;
            var distance = delta.magnitude;
            var step = _speed * Time.deltaTime;

            if (distance <= step)
            {
                transform.position = target;
                _cornerIndex++;
                if (_cornerIndex >= _corners.Count)
                {
                    _moving = false;
                    _marchLine?.SetActive(false);
                }
                else
                {
                    _marchLine?.SetPath(BuildMarchPath());
                }

                return;
            }

            transform.position = current + delta / distance * step;

            var lookDirection = new Vector3(delta.x, 0f, delta.z);
            if (lookDirection.sqrMagnitude > 1e-4f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(lookDirection),
                    Time.deltaTime * 12f);
            }

            // 起点每帧跟随角色，剩余拐点沿用服务端路径。
            _marchLine?.SetPath(BuildMarchPath());
        }

        /// <summary>玩家脚底 + 剩余拐点，构成当前行军线的世界坐标路径。</summary>
        private List<Vector3> BuildMarchPath()
        {
            _marchPath.Clear();
            _marchPath.Add(transform.position - Vector3.up * kVisualYOffset);

            for (int i = _cornerIndex; i < _corners.Count; i++)
                _marchPath.Add(_corners[i]);

            return _marchPath;
        }

        /// <summary>
        /// 异步加载并实例化行军线预制体。加载期间 <see cref="_marchLineLoading"/> 为 true，
        /// 完成后由 <see cref="OnMarchLineLoaded"/> 调 <see cref="RefreshMarchLine"/> 补上当前路径。
        /// </summary>
        private void EnsureMarchLine()
        {
            if (_marchLine != null || _marchLineLoading)
                return;

            if (FW.ResMgr == null)
            {
                Debug.LogError("[MarchLine] FW.ResMgr 未初始化，无法加载 " + kMarchLineAddress);
                return;
            }

            _marchLineLoading = true;
            try
            {
                var handle = FW.ResMgr.InstantiateAsync(kMarchLineAddress);
                handle.Completed += OnMarchLineLoaded;
            }
            catch (System.Exception ex)
            {
                _marchLineLoading = false;
                Debug.LogError("[MarchLine] 异步加载异常: " + ex.Message);
            }
        }

        private void OnMarchLineLoaded(AsyncOperationHandle<GameObject> handle)
        {
            _marchLineLoading = false;

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Debug.LogError("[MarchLine] 异步加载失败: " + kMarchLineAddress);
                FW.ResMgr?.Release(handle);
                return;
            }

            var go = handle.Result;

            // 加载期间玩家已被销毁：直接释放，别把实例留在场景里。
            if (_destroyed)
            {
                ReleaseMarchLine(go);
                return;
            }

            go.name = "MarchLine";
            _marchLine = go.GetComponent<MarchLine>();
            if (_marchLine == null)
            {
                Debug.LogError("[MarchLine] 预制体缺少 MarchLine 组件: " + kMarchLineAddress);
                ReleaseMarchLine(go);
                return;
            }

            RefreshMarchLine();
        }

        /// <summary>
        /// 按当前移动状态刷新行军线。路径响应到达时调用一次，异步加载完成时再调用一次，
        /// 保证加载期间收到的路径不会丢。
        /// </summary>
        private void RefreshMarchLine()
        {
            if (_marchLine == null)
                return;

            if (!_moving)
            {
                _marchLine.SetActive(false);
                return;
            }

            _marchLine.color = kMarchLineColor;
            _marchLine.Initialize(BuildMarchPath(), false);
            _marchLine.SetActive(true);
        }

        private void ReleaseMarchLine(GameObject go)
        {
            if (go == null)
                return;

            if (FW.ResMgr?.ReleaseInstance(go) ?? true)
                Destroy(go);
        }

        private void OnDestroy()
        {
            _destroyed = true;

            if (_marchLine == null)
                return;

            var go = _marchLine.gameObject;
            _marchLine = null;
            ReleaseMarchLine(go);
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(12f, 12f, 760f, 24f),
                "左键点击地面寻路；G 开关所有关卡门（服务端权威）");
        }

        private static Vec3 ToVec3(Vector3 value) => new Vec3
        {
            X = value.x,
            Y = value.y,
            Z = value.z
        };

        private static Vector3 Flat(Vector3 value) => new Vector3(value.x, 0f, value.z);
    }
}
