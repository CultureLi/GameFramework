using System.Collections.Generic;
using System.Linq;
using Framework;
using GameEntry;
using GameFramework.Level;
using Google.Protobuf.WellKnownTypes;
using UnityEngine;

namespace GameMain
{
    /// <summary>
    /// 正式寻路管理器（Main 场景）。
    ///
    /// 负责：
    ///   - 创建本地玩家；
    ///   - 接收 PathFindAck / MoveSync 并交给 LocalPlayerController；
    ///   - 接收服务端关卡门状态，创建/更新 LevelGate；
    ///   - 订阅 LevelGate.GateStateRequested，把手动开关请求发给服务端。
    /// </summary>
    public sealed class PathfindingMgr : GameMgrBase<PathfindingMgr>
    {
        private bool _initialized;
        private LocalPlayerController _player;
        private GameObject _gateRoot;
        private readonly Dictionary<int, LevelGate> _gates = new Dictionary<int, LevelGate>();

        public override void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;

            FW.NetMgr.RegisterMsg<PathFindAck>(OnPathFindAck);
            FW.NetMgr.RegisterMsg<MoveSync>(OnMoveSync);
            FW.NetMgr.RegisterMsg<GateStateSync>(OnGateStateSync);
            FW.NetMgr.RegisterMsg<SetMoveSpeedSync>(OnMoveSpeedSync);
            LevelGate.GateStateRequested += OnGateStateRequested;

            CreateLocalPlayer();

            // Main 场景加载完成后主动问服务端要一次所有关卡门状态
            FW.NetMgr.SendMsg(new GateStateQueryReq());

            Debug.Log("[Pathfinding] 初始化完成");
        }

        public override void Dispose()
        {
            if (!_initialized)
                return;

            _initialized = false;

            FW.NetMgr.UnregisterMsg<PathFindAck>(OnPathFindAck);
            FW.NetMgr.UnregisterMsg<MoveSync>(OnMoveSync);
            FW.NetMgr.UnregisterMsg<GateStateSync>(OnGateStateSync);
            FW.NetMgr.UnregisterMsg<SetMoveSpeedSync>(OnMoveSpeedSync);
            LevelGate.GateStateRequested -= OnGateStateRequested;

            if (_player != null)
                Object.Destroy(_player.gameObject);
            _player = null;

            if (_gateRoot != null)
                Object.Destroy(_gateRoot);
            _gateRoot = null;

            _gates.Clear();
        }

        /// <summary>供 UI / 调试手动请求某个关卡开关（服务端确认后会广播回来）。</summary>
        public void RequestGateState(int gateId, bool open)
        {
            if (!FW.NetMgr.IsConnected())
            {
                Debug.LogWarning("[Gate] 网络未连接，无法请求关卡状态");
                return;
            }

            FW.NetMgr.SendMsg(new GateStateReq { GateId = gateId, Open = open });
        }

        /// <summary>请求切换某个关卡门（按服务端同步下来的当前状态取反）。</summary>
        public void RequestGateToggle(int gateId)
        {
            LevelGate gate = null;
            if (!_gates.TryGetValue(gateId, out gate) || gate == null)
            {
                gate = Object.FindObjectsOfType<LevelGate>(true)
                    .FirstOrDefault(g => g.gateID == gateId);
                if (gate == null)
                {
                    Debug.LogWarning($"[Gate] 找不到关卡 {gateId}，无法切换");
                    return;
                }
                _gates[gateId] = gate;
            }

            RequestGateState(gateId, !gate.IsOpen);
        }

        /// <summary>请求设置本地玩家移动速度（服务端确认后广播回来）。</summary>
        public void RequestMoveSpeed(float speed)
        {
            if (!FW.NetMgr.IsConnected())
            {
                Debug.LogWarning("[Speed] 网络未连接，无法请求移动速度");
                return;
            }

            FW.NetMgr.SendMsg(new SetMoveSpeedReq { Speed = Mathf.Clamp(speed, 1f, 20f) });
        }

        private void CreateLocalPlayer()
        {
            var spawn = LoginSession.SpawnPosition;
            var playerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerObject.name = "LocalPlayer";

            _player = playerObject.AddComponent<LocalPlayerController>();
            _player.Initialize(LoginSession.PlayerId, spawn);

            Debug.Log($"[Pathfinding] 创建本地玩家 playerId={LoginSession.PlayerId} spawn={spawn}");
        }

        private void OnPathFindAck(PathFindAck ack)
        {
            _player?.OnPathFindAck(ack);
        }

        private void OnMoveSync(MoveSync sync)
        {
            if (sync.PlayerId == LoginSession.PlayerId)
                _player?.OnMoveSync(sync);
        }

        private void OnGateStateRequested(int gateId, bool open)
        {
            RequestGateState(gateId, open);
        }

        private void OnGateStateSync(GateStateSync sync)
        {
            if (sync.Code != 0)
            {
                Debug.LogWarning($"[Gate] 关卡状态同步失败 gateId={sync.GateId} code={sync.Code}");
                return;
            }

            var gate = GetOrCreateGate(sync);
            if (gate == null)
                return;

            gate.SetOpen(sync.Open);
        }

        private void OnMoveSpeedSync(SetMoveSpeedSync sync)
        {
            if (sync.Code != 0)
            {
                Debug.LogWarning($"[Speed] 移动速度同步失败 code={sync.Code}");
                return;
            }

            if (sync.PlayerId != LoginSession.PlayerId)
                return;

            _player?.SetMoveSpeed(sync.Speed);
            Debug.Log($"[Speed] 服务端确认移动速度 = {sync.Speed:0.##}");
        }

        private LevelGate GetOrCreateGate(GateStateSync sync)
        {
            if (_gates.TryGetValue(sync.GateId, out var existing))
                return existing;

            // Main 场景里可能已经手动摆了 LevelGate，优先复用
            var sceneGate = Object.FindObjectsOfType<LevelGate>(true)
                .FirstOrDefault(g => g.gateID == sync.GateId);
            if (sceneGate != null)
            {
                _gates[sync.GateId] = sceneGate;
                return sceneGate;
            }

            if (sync.Center == null || sync.Size == null)
            {
                Debug.LogWarning($"[Gate] 关卡 {sync.GateId} 缺少位置/尺寸，无法创建");
                return null;
            }

            if (_gateRoot == null)
            {
                _gateRoot = new GameObject("Gates_Dynamic");
                _gateRoot.AddComponent<MazeGateDebugToggle>();
            }

            var gateObject = new GameObject($"Gate_{sync.GateId}");
            gateObject.transform.SetParent(_gateRoot.transform, false);
            gateObject.transform.position = new Vector3(sync.Center.X, sync.Center.Y, sync.Center.Z);
            gateObject.transform.rotation = Quaternion.Euler(0f, sync.RotationY, 0f);

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            visual.transform.SetParent(gateObject.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(sync.Size.X, sync.Size.Y, sync.Size.Z);
            Object.Destroy(visual.GetComponent<Collider>());
            var renderer = visual.GetComponent<MeshRenderer>();

            var gate = gateObject.AddComponent<LevelGate>();
            gate.gateID = sync.GateId;
            gate.gateRenderer = renderer;
            gate.SetOpen(sync.Open);

            _gates[sync.GateId] = gate;
            Debug.Log($"[Gate] 创建动态关卡门 {sync.GateId}");
            return gate;
        }
    }
}
