using System.Collections.Generic;
using System.Linq;
using Framework;
using GameEntry;
using GameFramework.Level;
using Google.Protobuf.WellKnownTypes;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        private GateInfoPanel _gatePanel;
        private readonly Dictionary<int, LevelGate> _gates = new Dictionary<int, LevelGate>();
        private LocalPathfindingService _localPathfinding;

        public LocalPathfindingService LocalPathfinding => _localPathfinding;

        private static bool IsSinglePlayer => GameModeSettings.IsSinglePlayer;

        public override void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;

            if (!IsSinglePlayer)
            {
                FW.NetMgr.RegisterMsg<PathFindAck>(OnPathFindAck);
                FW.NetMgr.RegisterMsg<MoveSync>(OnMoveSync);
                FW.NetMgr.RegisterMsg<GateStateSync>(OnGateStateSync);
                FW.NetMgr.RegisterMsg<SetMoveSpeedSync>(OnMoveSpeedSync);
                FW.NetMgr.RegisterMsg<GateInfoAck>(OnGateInfoAck);
                FW.NetMgr.RegisterMsg<GateApplyAck>(OnGateApplyAck);
                FW.NetMgr.RegisterMsg<GateApproveAck>(OnGateApproveAck);
                FW.NetMgr.RegisterMsg<GateRemoveAccessAck>(OnGateRemoveAccessAck);
                FW.NetMgr.RegisterMsg<GateClaimAck>(OnGateClaimAck);
                FW.NetMgr.RegisterMsg<GateAccessSync>(OnGateAccessSync);
            }
            else
            {
                _localPathfinding = LocalPathfindingService.Create(ResolveMapName());
            }

            LevelGate.GateStateRequested += OnGateStateRequested;

            CreateLocalPlayer();

            var panelObject = new GameObject("GateInfoPanel");
            _gatePanel = panelObject.AddComponent<GateInfoPanel>();
            _gatePanel.Bind(this);
            _gatePanel.Hide();

            if (!IsSinglePlayer)
            {
                // Main 场景加载完成后主动问服务端要一次所有关卡门状态
                FW.NetMgr.SendMsg(new GateStateQueryReq());
            }

            Debug.Log($"[Pathfinding] 初始化完成 mode={(IsSinglePlayer ? "单机" : "联机")}");
        }

        public override void Dispose()
        {
            if (!_initialized)
                return;

            _initialized = false;

            if (!IsSinglePlayer)
            {
                FW.NetMgr.UnregisterMsg<PathFindAck>(OnPathFindAck);
                FW.NetMgr.UnregisterMsg<MoveSync>(OnMoveSync);
                FW.NetMgr.UnregisterMsg<GateStateSync>(OnGateStateSync);
                FW.NetMgr.UnregisterMsg<SetMoveSpeedSync>(OnMoveSpeedSync);
                FW.NetMgr.UnregisterMsg<GateInfoAck>(OnGateInfoAck);
                FW.NetMgr.UnregisterMsg<GateApplyAck>(OnGateApplyAck);
                FW.NetMgr.UnregisterMsg<GateApproveAck>(OnGateApproveAck);
                FW.NetMgr.UnregisterMsg<GateRemoveAccessAck>(OnGateRemoveAccessAck);
                FW.NetMgr.UnregisterMsg<GateClaimAck>(OnGateClaimAck);
                FW.NetMgr.UnregisterMsg<GateAccessSync>(OnGateAccessSync);
            }

            LevelGate.GateStateRequested -= OnGateStateRequested;

            if (_gatePanel != null)
                Object.Destroy(_gatePanel.gameObject);
            _gatePanel = null;

            if (_player != null)
                Object.Destroy(_player.gameObject);
            _player = null;

            if (_gateRoot != null)
                Object.Destroy(_gateRoot);
            _gateRoot = null;

            _gates.Clear();
            _localPathfinding?.Dispose();
            _localPathfinding = null;
        }

        /// <summary>供 UI / 调试手动请求某个关卡开关（服务端确认后会广播回来）。</summary>
        public void RequestGateState(int gateId, bool open)
        {
            if (IsSinglePlayer)
            {
                if (_localPathfinding == null || !_localPathfinding.TrySetGlobalGateState(gateId, open))
                {
                    Debug.LogWarning($"[Gate] 单机模式下找不到关卡 {gateId}");
                    return;
                }

                var sceneGates = Object.FindObjectsOfType<LevelGate>(true);
                for (int i = 0; i < sceneGates.Length; i++)
                {
                    if (sceneGates[i].gateID == gateId)
                        sceneGates[i].SetOpen(open);
                }

                Debug.Log($"[Gate] 单机关卡 {gateId} -> {(open ? "打开" : "关闭")}");
                return;
            }

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
            speed = Mathf.Clamp(speed, 1f, 20f);

            if (IsSinglePlayer)
            {
                _player?.SetMoveSpeed(speed);
                Debug.Log($"[Speed] 单机模式移动速度 = {speed:0.##}");
                return;
            }

            if (!FW.NetMgr.IsConnected())
            {
                Debug.LogWarning("[Speed] 网络未连接，无法请求移动速度");
                return;
            }

            FW.NetMgr.SendMsg(new SetMoveSpeedReq { Speed = speed });
        }

        public void RequestGateInfo(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
                return;

            if (IsSinglePlayer)
            {
                var info = _localPathfinding?.GetGateInfo(instanceId);
                if (info == null)
                {
                    Debug.LogWarning("[Gate] 单机模式下找不到关卡: " + instanceId);
                    return;
                }

                _gatePanel?.Show(BuildGateInfoAck(info));
                return;
            }

            if (!FW.NetMgr.IsConnected())
            {
                Debug.LogWarning("[Gate] 网络未连接，无法请求关卡信息");
                return;
            }

            FW.NetMgr.SendMsg(new GateInfoReq { InstanceId = instanceId });
        }

        public void ApplyGate(string instanceId)
        {
            if (IsSinglePlayer)
            {
                ApplyLocalGateResult(_localPathfinding?.ApplyGate(instanceId));
                return;
            }

            if (!FW.NetMgr.IsConnected())
                return;

            FW.NetMgr.SendMsg(new GateApplyReq { InstanceId = instanceId });
        }

        public void ApproveGate(string instanceId, ulong playerId)
        {
            if (IsSinglePlayer)
            {
                ApplyLocalGateResult(_localPathfinding?.ApproveGate(instanceId, playerId));
                return;
            }

            if (!FW.NetMgr.IsConnected())
                return;

            FW.NetMgr.SendMsg(new GateApproveReq { InstanceId = instanceId, PlayerId = playerId });
        }

        public void RemoveGateAccess(string instanceId, ulong playerId)
        {
            if (IsSinglePlayer)
            {
                ApplyLocalGateResult(_localPathfinding?.RemoveGateAccess(instanceId, playerId));
                return;
            }

            if (!FW.NetMgr.IsConnected())
                return;

            FW.NetMgr.SendMsg(new GateRemoveAccessReq { InstanceId = instanceId, PlayerId = playerId });
        }

        public void ClaimGate(string instanceId)
        {
            if (IsSinglePlayer)
            {
                ApplyLocalGateResult(_localPathfinding?.ClaimGate(instanceId));
                return;
            }

            if (!FW.NetMgr.IsConnected())
                return;

            FW.NetMgr.SendMsg(new GateClaimReq { InstanceId = instanceId });
        }

        private static string ResolveMapName()
        {
            var streaming = Object.FindObjectOfType<WorldChunkStreaming>();
            if (streaming != null && !string.IsNullOrEmpty(streaming.sceneName))
                return streaming.sceneName;

            var scene = SceneManager.GetActiveScene();
            return string.IsNullOrEmpty(scene.name) ? "OpenWorld" : scene.name;
        }

        private void CreateLocalPlayer()
        {
            Vector3 spawn;
            ulong playerId;

            if (IsSinglePlayer)
            {
                spawn = _localPathfinding != null ? _localPathfinding.SpawnPoint : Vector3.zero;
                playerId = LocalPathfindingService.LocalPlayerId;
            }
            else
            {
                spawn = LoginSession.SpawnPosition;
                playerId = LoginSession.PlayerId;
            }

            var playerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerObject.name = "LocalPlayer";

            _player = playerObject.AddComponent<LocalPlayerController>();
            _player.Initialize(playerId, spawn);

            Debug.Log($"[Pathfinding] 创建本地玩家 playerId={playerId} spawn={spawn} " +
                      $"mode={(IsSinglePlayer ? "单机" : "联机")}");
        }

        /// <summary>
        /// 单机模式：MapDataMgr 加载完 BuildingData 后注册建筑关卡。
        /// </summary>
        public void SetLocalBuildingData(BuildingData data)
        {
            if (!IsSinglePlayer || _localPathfinding == null)
                return;

            _localPathfinding.SetBuildingData(data);
        }

        /// <summary>单机模式：直接用客户端 Recast 计算路径。</summary>
        public void RequestLocalPath(Vector3 target)
        {
            if (!IsSinglePlayer || _localPathfinding == null || _player == null)
                return;

            if (!_localPathfinding.GatesReady)
                Debug.LogWarning("[Recast] 关卡数据尚未加载，本次寻路暂不检查关卡通行权限");

            if (!_localPathfinding.FindPath(_player.FeetPosition, target, out var corners, out var error))
            {
                Debug.LogWarning("[Path] 单机寻路失败: " + error);
                return;
            }

            _player.ApplyLocalPath(corners);
            Debug.Log($"[Path] 单机寻路完成 corners={corners.Count}");
        }

        private void ApplyLocalGateResult(GateOperationResult result)
        {
            if (result == null)
            {
                _gatePanel?.SetMessage("本地关卡数据未加载");
                return;
            }

            if (result.Code != GateResultCode.Success)
            {
                _gatePanel?.SetMessage(result.Message);
                return;
            }

            if (result.Snapshot != null)
                _gatePanel?.ApplySync(BuildGateSync(result.Snapshot));

            _gatePanel?.SetMessage(result.Message);
        }

        private static GateInfoAck BuildGateInfoAck(GateInfoSnapshot info)
        {
            var ack = new GateInfoAck
            {
                Code = GateResultCode.Success,
                InstanceId = info.InstanceId,
                OwnerPlayerId = info.OwnerPlayerId,
                IsOwner = info.MyStatus == GateStatus.Owner,
                MyStatus = info.MyStatus,
            };

            for (int i = 0; i < info.AllowedPlayers.Count; i++)
                ack.AllowedPlayers.Add(new GatePlayerInfo { PlayerId = info.AllowedPlayers[i] });

            for (int i = 0; i < info.PendingRequests.Count; i++)
                ack.PendingRequests.Add(new GatePlayerInfo { PlayerId = info.PendingRequests[i] });

            return ack;
        }

        private static GateAccessSync BuildGateSync(GateInfoSnapshot info)
        {
            var sync = new GateAccessSync
            {
                Code = GateResultCode.Success,
                InstanceId = info.InstanceId,
                OwnerPlayerId = info.OwnerPlayerId,
                MyStatus = info.MyStatus,
            };

            for (int i = 0; i < info.AllowedPlayers.Count; i++)
                sync.AllowedPlayers.Add(new GatePlayerInfo { PlayerId = info.AllowedPlayers[i] });

            for (int i = 0; i < info.PendingRequests.Count; i++)
                sync.PendingRequests.Add(new GatePlayerInfo { PlayerId = info.PendingRequests[i] });

            return sync;
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

        private void OnGateInfoAck(GateInfoAck ack)
        {
            if (ack.Code != 0)
            {
                Debug.LogWarning($"[Gate] 获取关卡信息失败 code={ack.Code} message={ack.Message}");
                return;
            }

            _gatePanel?.Show(ack);
        }

        private void OnGateApplyAck(GateApplyAck ack)
        {
            if (ack.Code != 0)
            {
                Debug.LogWarning($"[Gate] 申请通行失败 code={ack.Code} message={ack.Message}");
                _gatePanel?.SetMessage(ack.Message);
                return;
            }

            _gatePanel?.ApplyStatus(ack.Status, ack.Message);
        }

        private void OnGateApproveAck(GateApproveAck ack)
        {
            if (ack.Code != 0)
                Debug.LogWarning($"[Gate] 审批失败 code={ack.Code} message={ack.Message}");
            else
                _gatePanel?.SetMessage(ack.Message);
        }

        private void OnGateRemoveAccessAck(GateRemoveAccessAck ack)
        {
            if (ack.Code != 0)
                Debug.LogWarning($"[Gate] 移除通行权失败 code={ack.Code} message={ack.Message}");
            else
                _gatePanel?.SetMessage(ack.Message);
        }

        private void OnGateClaimAck(GateClaimAck ack)
        {
            if (ack.Code != 0)
                Debug.LogWarning($"[Gate] 认领关卡失败 code={ack.Code} message={ack.Message}");
            else
                _gatePanel?.SetMessage(ack.Message);
        }

        private void OnGateAccessSync(GateAccessSync sync)
        {
            _gatePanel?.ApplySync(sync);
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
