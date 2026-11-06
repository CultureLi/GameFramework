using GameMain;
using GameServer.Map;

namespace GameServer.Game;

/// <summary>
/// 每个玩家本次会话使用的关卡通行位图。由 GateAccessStore.BuildMask 生成，
/// 只用于 DotRecast 查询过滤，不持久化。
/// </summary>
public sealed class GateAccessMask
{
    private readonly ulong[] _bits;

    public GateAccessMask(int gateCount)
    {
        _bits = new ulong[Math.Max(1, (gateCount + 63) / 64)];
    }

    public void Set(int gateIndex)
    {
        if (gateIndex < 0)
            return;

        int chunk = gateIndex >> 6;
        if (chunk >= _bits.Length)
            return;

        _bits[chunk] |= 1UL << (gateIndex & 63);
    }

    public bool IsAllowed(int gateIndex)
    {
        if (gateIndex < 0)
            return false;

        int chunk = gateIndex >> 6;
        if (chunk >= _bits.Length)
            return false;

        return (_bits[chunk] & (1UL << (gateIndex & 63))) != 0;
    }

    public bool ContentEquals(GateAccessMask other)
    {
        if (other == null || _bits.Length != other._bits.Length)
            return false;

        for (int i = 0; i < _bits.Length; i++)
        {
            if (_bits[i] != other._bits[i])
                return false;
        }

        return true;
    }
}

public sealed class GateInfoSnapshot
{
    public required string InstanceId;
    public ulong OwnerPlayerId;
    public int MyStatus;
    public List<ulong> AllowedPlayers = new();
    public List<ulong> PendingRequests = new();
}

public sealed class GateOperationResult
{
    public int Code;
    public int Status;
    public string Message = string.Empty;
    public GateInfoSnapshot? Snapshot;
}

/// <summary>
/// 建筑关卡的归属/申请/白名单。当前为内存实现：
/// 玩家离线期间的状态仍保留在服务器进程内；玩家上线时重新 BuildMask。
/// 正式服应替换成数据库读写，key 使用 mapId + instanceId。
/// </summary>
public sealed class GateAccessStore
{
    private sealed class GateState
    {
        public required string InstanceId;
        public int ConfigId;
        public int GateIndex;
        public ulong OwnerPlayerId;
        public readonly HashSet<ulong> AllowedPlayers = new();
        public readonly HashSet<ulong> PendingRequests = new();
        public readonly HashSet<ulong> RejectedPlayers = new();
    }

    private readonly object _sync = new();
    private readonly Dictionary<string, GateState> _gates = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _indices = new(StringComparer.Ordinal);
    private readonly List<GateState> _ordered = new();

    public GateAccessStore(IReadOnlyList<MapGateDefinition> definitions)
    {
        foreach (var definition in definitions)
        {
            var state = new GateState
            {
                InstanceId = definition.InstanceId,
                ConfigId = definition.ConfigId,
                GateIndex = _ordered.Count,
            };
            definition.GateIndex = state.GateIndex;
            _gates[state.InstanceId] = state;
            _indices[state.InstanceId] = state.GateIndex;
            _ordered.Add(state);
        }
    }

    public int GateCount => _ordered.Count;

    public IReadOnlyDictionary<string, int> GateIndices => _indices;

    /// <summary>
    /// 根据服务端权限状态重新生成玩家位图。未归属关卡不允许任何人通行，
    /// 必须先认领或由已有归属系统设置 owner。
    /// </summary>
    public GateAccessMask BuildMask(ulong playerId)
    {
        lock (_sync)
        {
            var mask = new GateAccessMask(_ordered.Count);
            foreach (var gate in _ordered)
            {
                if (gate.OwnerPlayerId != 0 &&
                    (gate.OwnerPlayerId == playerId ||
                     gate.AllowedPlayers.Contains(playerId)))
                {
                    mask.Set(gate.GateIndex);
                }
            }

            return mask;
        }
    }

    public GateInfoSnapshot? GetInfo(string instanceId, ulong playerId, bool includeLists)
    {
        lock (_sync)
        {
            if (!_gates.TryGetValue(instanceId, out var gate))
                return null;

            var snapshot = new GateInfoSnapshot
            {
                InstanceId = gate.InstanceId,
                OwnerPlayerId = gate.OwnerPlayerId,
                MyStatus = GetStatus(gate, playerId),
            };

            if (includeLists)
            {
                snapshot.AllowedPlayers.AddRange(gate.AllowedPlayers);
                snapshot.PendingRequests.AddRange(gate.PendingRequests);
            }

            return snapshot;
        }
    }

    public GateOperationResult Apply(string instanceId, ulong playerId)
    {
        lock (_sync)
        {
            if (!_gates.TryGetValue(instanceId, out var gate))
                return Fail(GateResultCode.NotFound, "关卡不存在");

            int status = GetStatus(gate, playerId);
            if (status == GateStatus.Unowned)
                return Ok(GateStatus.Unowned, gate, "该关卡未归属，当前禁止通行，请先认领");

            if (status == GateStatus.Owner)
                return Ok(GateStatus.Owner, gate, "你是该关卡的归属人");

            if (status == GateStatus.Approved)
                return Ok(GateStatus.Approved, gate, "你已可以通行该关卡");

            if (status == GateStatus.Pending)
                return Ok(GateStatus.Pending, gate, "已申请，等待归属人处理");

            gate.RejectedPlayers.Remove(playerId);
            gate.PendingRequests.Add(playerId);
            return Ok(GateStatus.Pending, gate, "申请已发送，等待归属人处理");
        }
    }

    public GateOperationResult Approve(string instanceId, ulong ownerPlayerId, ulong targetPlayerId)
    {
        lock (_sync)
        {
            if (!_gates.TryGetValue(instanceId, out var gate))
                return Fail(GateResultCode.NotFound, "关卡不存在");

            if (gate.OwnerPlayerId == 0)
                return Fail(GateResultCode.InvalidState, "关卡还没有归属人");

            if (gate.OwnerPlayerId != ownerPlayerId)
                return Fail(GateResultCode.NotOwner, "只有归属人可以审批");

            if (targetPlayerId == ownerPlayerId)
                return Fail(GateResultCode.InvalidParam, "归属人默认可以通行");

            gate.PendingRequests.Remove(targetPlayerId);
            gate.RejectedPlayers.Remove(targetPlayerId);
            gate.AllowedPlayers.Add(targetPlayerId);
            return Ok(GateStatus.Approved, gate, "已同意通行");
        }
    }

    public GateOperationResult RemoveAccess(string instanceId, ulong ownerPlayerId, ulong targetPlayerId)
    {
        lock (_sync)
        {
            if (!_gates.TryGetValue(instanceId, out var gate))
                return Fail(GateResultCode.NotFound, "关卡不存在");

            if (gate.OwnerPlayerId == 0)
                return Fail(GateResultCode.InvalidState, "关卡还没有归属人");

            if (gate.OwnerPlayerId != ownerPlayerId)
                return Fail(GateResultCode.NotOwner, "只有归属人可以移除通行权");

            if (targetPlayerId == ownerPlayerId)
                return Fail(GateResultCode.InvalidParam, "不能移除归属人自己");

            gate.AllowedPlayers.Remove(targetPlayerId);
            gate.PendingRequests.Remove(targetPlayerId);
            gate.RejectedPlayers.Add(targetPlayerId);
            return Ok(GateStatus.Rejected, gate, "已移除通行权");
        }
    }

    public GateOperationResult Claim(string instanceId, ulong playerId)
    {
        lock (_sync)
        {
            if (!_gates.TryGetValue(instanceId, out var gate))
                return Fail(GateResultCode.NotFound, "关卡不存在");

            if (gate.OwnerPlayerId != 0)
                return Fail(GateResultCode.InvalidState, "关卡已经有归属人");

            gate.OwnerPlayerId = playerId;
            gate.AllowedPlayers.Clear();
            gate.RejectedPlayers.Clear();
            gate.PendingRequests.Remove(playerId);
            return Ok(GateStatus.Owner, gate, "已成为关卡归属人");
        }
    }

    /// <summary>
    /// 供已有的建造/占领系统设置归属。设置后需要调用 RefreshAllGateMasks 刷新在线玩家位图；
    /// 离线玩家下次登录时重新 BuildMask 即可。
    /// </summary>
    public bool SetOwner(string instanceId, ulong ownerPlayerId)
    {
        lock (_sync)
        {
            if (!_gates.TryGetValue(instanceId, out var gate))
                return false;

            gate.OwnerPlayerId = ownerPlayerId;
            gate.AllowedPlayers.Clear();
            gate.RejectedPlayers.Clear();
            gate.PendingRequests.Remove(ownerPlayerId);
            return true;
        }
    }

    private static int GetStatus(GateState gate, ulong playerId)
    {
        if (gate.OwnerPlayerId == 0)
            return GateStatus.Unowned;

        if (gate.OwnerPlayerId == playerId)
            return GateStatus.Owner;

        if (gate.AllowedPlayers.Contains(playerId))
            return GateStatus.Approved;

        if (gate.PendingRequests.Contains(playerId))
            return GateStatus.Pending;

        if (gate.RejectedPlayers.Contains(playerId))
            return GateStatus.Rejected;

        return GateStatus.None;
    }

    private GateOperationResult Ok(int status, GateState gate, string message)
    {
        return new GateOperationResult
        {
            Code = GateResultCode.Success,
            Status = status,
            Message = message,
            Snapshot = new GateInfoSnapshot
            {
                InstanceId = gate.InstanceId,
                OwnerPlayerId = gate.OwnerPlayerId,
                MyStatus = status,
                AllowedPlayers = new List<ulong>(gate.AllowedPlayers),
                PendingRequests = new List<ulong>(gate.PendingRequests),
            },
        };
    }

    private static GateOperationResult Fail(int code, string message)
    {
        return new GateOperationResult
        {
            Code = code,
            Message = message,
        };
    }
}
