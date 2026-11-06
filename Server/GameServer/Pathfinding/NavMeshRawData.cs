namespace GameServer.Pathfinding;

/// <summary>
/// Unity NavMeshExporter 导出的 .rnav 数据。
/// 格式见 RecastNavigationServer/DATA_FORMAT.md。
/// </summary>
public sealed class NavMeshRawData
{
    public float AgentHeight;
    public float AgentRadius;
    public float AgentMaxClimb;
    public float CellSize;
    public float CellHeight;

    public float[] Vertices = Array.Empty<float>();
    public int[] Indices = Array.Empty<int>();
    public byte[] Areas = Array.Empty<byte>();
    public List<NavMeshLinkData> Links = new();
    /// <summary>.rnav v2 动态关卡门。</summary>
    public List<NavMeshGateData> Gates = new();

    public int VertexCount => Vertices.Length / 3;
    public int TriangleCount => Indices.Length / 3;
}

public sealed class NavMeshLinkData
{
    public float[] StartPos = new float[3];
    public float[] EndPos = new float[3];
    public float Radius;
    public bool Bidirectional;
    public byte Area;
}

public sealed class NavMeshGateData
{
    public int GateId;
    public float[] Center = new float[3];
    public float[] Size = new float[3];
    public float RotationY;
    public bool Open;
}
