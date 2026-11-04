using System.Collections.Generic;
using System.Linq;
using Framework;
using GameEntry;
using Google.Protobuf.WellKnownTypes;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

namespace GameMain
{
    /// <summary>
    /// 寻路 Demo。
    ///
    /// 流程：
    ///   1. 左键点击地面，发送 PathFindReq；
    ///   2. 服务端用 DotRecast 在 navmesh.rnav 上寻路，返回 PathFindAck（corners）；
    ///   3. 服务端按 tick 移动角色，持续下发 MoveSync；
    ///   4. 客户端沿 corners 做本地预测，并用 MoveSync 校正位置。
    /// </summary>
    public sealed class PathFindingDemo : MonoBehaviour
    {
        private const float kVisualYOffset = 1f;
        private const float kSyncCorrectionThreshold = 0.75f;

        private readonly List<Vector3> _corners = new();
        private Transform _player;
        private Camera _camera;
        private int _cornerIndex;
        private uint _pathId;
        private uint _seq;
        private float _speed = 4f;
        private bool _moving;
        private Vector3 _serverPosition;

        private void Start()
        {
            FW.NetMgr.RegisterMsg<PathFindAck>(OnPathFindAck);
            FW.NetMgr.RegisterMsg<MoveSync>(OnMoveSync);

            EnsureEventSystem();
            SetupPlayer();
            SetupCamera();
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private void OnDestroy()
        {
            FW.NetMgr.UnregisterMsg<PathFindAck>(OnPathFindAck);
            FW.NetMgr.UnregisterMsg<MoveSync>(OnMoveSync);
        }

        private void SetupPlayer()
        {
            // PathFinding 场景里的 Agent 默认隐藏；激活它并禁用 NavMeshAgent，
            // 移动完全由服务端的寻路结果和位置同步驱动。
            var agents = FindObjectsOfType<NavMeshAgent>(true);
            var agent = agents.FirstOrDefault(a => a.name == "Agent") ?? agents.FirstOrDefault();

            if (agent != null)
            {
                agent.enabled = false;
                _player = agent.transform;
                _player.gameObject.SetActive(true);
            }
            else
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = "LocalPlayer";
                _player = go.transform;
            }

            var spawn = LoginSession.SpawnPosition;
            _player.position = new Vector3(spawn.x, spawn.y + kVisualYOffset, spawn.z);
            _player.rotation = Quaternion.Euler(0f, LoginSession.SpawnRotationY, 0f);
        }

        private void SetupCamera()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                var go = new GameObject("Pathfinding Camera");
                _camera = go.AddComponent<Camera>();
                go.tag = "MainCamera";
            }
            else
            {
                // 关掉测试场景自带的相机控制脚本，位置由这里接管
                foreach (var behaviour in _camera.GetComponents<MonoBehaviour>())
                    behaviour.enabled = false;
            }

            UpdateCamera();
        }

        private void Update()
        {
            if (_player == null)
                return;

            HandleClick();
            UpdateLocalMovement();
        }

        private void LateUpdate()
        {
            UpdateCamera();
        }

        private void HandleClick()
        {
            if (!Input.GetMouseButtonDown(0))
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, 200f))
                return;

            if (hit.transform == _player || hit.transform.IsChildOf(_player))
                return;

            _seq++;
            var target = hit.point;

            FW.NetMgr.SendMsg(new PathFindReq
            {
                Seq = _seq,
                Target = ToVec3(target),
                ClientPos = ToVec3(_player.position - Vector3.up * kVisualYOffset)
            });

            Debug.Log($"[Path] 发送寻路请求 seq={_seq} target={target}");
        }

        private void OnPathFindAck(PathFindAck ack)
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
                _player.position = new Vector3(
                    ack.Start.X,
                    ack.Start.Y + kVisualYOffset,
                    ack.Start.Z);
            }

            _cornerIndex = _corners.Count > 1 ? 1 : _corners.Count;
            _moving = _cornerIndex < _corners.Count;

            Debug.Log($"[Path] 收到路径 pathId={ack.PathId} corners={_corners.Count} speed={_speed}");
        }

        private void OnMoveSync(MoveSync sync)
        {
            if (sync.PlayerId != LoginSession.PlayerId || sync.Position == null)
                return;

            _serverPosition = new Vector3(sync.Position.X, sync.Position.Y, sync.Position.Z);

            if (!sync.Moving && sync.PathId == _pathId)
            {
                _moving = false;
                _cornerIndex = _corners.Count;
                _player.position = _serverPosition + Vector3.up * kVisualYOffset;
                _player.rotation = Quaternion.Euler(0f, sync.RotationY, 0f);
                return;
            }

            if (sync.PathId == _pathId)
            {
                var localFeet = _player.position - Vector3.up * kVisualYOffset;
                if (Vector3.Distance(Flat(localFeet), Flat(_serverPosition)) > kSyncCorrectionThreshold)
                {
                    // 本地预测和服务端位置偏差过大时平滑拉回
                    _player.position = Vector3.Lerp(
                        _player.position,
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
            var current = _player.position;
            var delta = target - current;
            var distance = delta.magnitude;
            var step = _speed * Time.deltaTime;

            if (distance <= step)
            {
                _player.position = target;
                _cornerIndex++;
                if (_cornerIndex >= _corners.Count)
                    _moving = false;
                return;
            }

            _player.position = current + delta / distance * step;

            var lookDirection = new Vector3(delta.x, 0f, delta.z);
            if (lookDirection.sqrMagnitude > 1e-4f)
            {
                _player.rotation = Quaternion.Slerp(
                    _player.rotation,
                    Quaternion.LookRotation(lookDirection),
                    Time.deltaTime * 12f);
            }
        }

        private void UpdateCamera()
        {
            if (_camera == null || _player == null)
                return;

            var target = _player.position;
            _camera.transform.position = target + new Vector3(0f, 18f, -12f);
            _camera.transform.LookAt(target + Vector3.up * 0.5f);
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(12f, 12f, 760f, 24f),
                "左键点击地面寻路；服务端寻路并移动，客户端接收 PathFindAck / MoveSync");
        }

        private static Vec3 ToVec3(Vector3 value) => new()
        {
            X = value.x,
            Y = value.y,
            Z = value.z
        };

        private static Vector3 Flat(Vector3 value) => new(value.x, 0f, value.z);
    }
}
