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
