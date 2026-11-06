using System.Collections.Generic;
using Framework;
using GameEntry;
using Google.Protobuf.WellKnownTypes;
using UnityEngine;
using UnityEngine.EventSystems;

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

        private readonly List<Vector3> _corners = new List<Vector3>();

        private ulong _playerId;
        private Vector3 _spawn;
        private uint _pathId;
        private uint _seq;
        private float _speed = 4f;
        private bool _moving;
        private int _cornerIndex;
        private Vector3 _serverPosition;

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
                    _moving = false;
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
