namespace GameServer.Game;

public readonly record struct PathPoint(float X, float Y, float Z);

/// <summary>
/// 服务端玩家实体。位置是脚底坐标（导航网格表面），客户端显示时再抬高到角色中心。
/// </summary>
public sealed class PlayerEntity
{
    private List<PathPoint> _path = new();

    public PlayerEntity(ulong playerId, PathPoint spawn)
    {
        PlayerId = playerId;
        X = spawn.X;
        Y = spawn.Y;
        Z = spawn.Z;
    }

    public ulong PlayerId { get; }

    public float X { get; private set; }
    public float Y { get; private set; }
    public float Z { get; private set; }
    public float RotationY { get; private set; }

    public float Speed { get; private set; } = 4f;
    public uint PathId { get; private set; }
    public int PathIndex { get; private set; }

    public bool Moving => PathIndex < _path.Count;

    public IReadOnlyList<PathPoint> Path => _path;

    public void SetPosition(PathPoint position)
    {
        X = position.X;
        Y = position.Y;
        Z = position.Z;
    }

    /// <summary>
    /// 设置新路径。路径第一个点是服务端吸附后的起点，移动从第二个点开始。
    /// </summary>
    public uint SetPath(IReadOnlyList<PathPoint> corners, float speed)
    {
        PathId++;
        Speed = speed;
        _path = corners.ToList();
        PathIndex = _path.Count > 1 ? 1 : _path.Count;

        if (_path.Count > 0)
            SetPosition(_path[0]);

        return PathId;
    }

    /// <summary>
    /// 按固定速度沿路径推进。dt 是秒。
    /// </summary>
    public void Update(float dt)
    {
        if (dt <= 0f || !Moving)
            return;

        var remaining = dt;
        while (remaining > 0f && PathIndex < _path.Count)
        {
            var target = _path[PathIndex];
            var dx = target.X - X;
            var dy = target.Y - Y;
            var dz = target.Z - Z;
            var distance = MathF.Sqrt(dx * dx + dy * dy + dz * dz);

            if (distance <= 1e-4f)
            {
                X = target.X;
                Y = target.Y;
                Z = target.Z;
                PathIndex++;
                continue;
            }

            var step = Speed * remaining;
            if (step >= distance)
            {
                X = target.X;
                Y = target.Y;
                Z = target.Z;
                PathIndex++;
                remaining -= distance / Speed;
                continue;
            }

            var ratio = step / distance;
            X += dx * ratio;
            Y += dy * ratio;
            Z += dz * ratio;
            RotationY = MathF.Atan2(dx, dz) * (180f / MathF.PI);
            remaining = 0f;
        }
    }
}
