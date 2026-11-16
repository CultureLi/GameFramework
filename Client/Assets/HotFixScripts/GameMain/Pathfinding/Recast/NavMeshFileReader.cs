using System.IO;
using System.Text;

namespace GameMain
{
    /// <summary>
    /// .rnav 文件读取，对应服务器 GameServer/Pathfinding/NavMeshFileReader.cs。
    /// 客户端支持从文件或内存字节读取（Resources 里的 TextAsset.bytes）。
    /// </summary>
    public static class NavMeshFileReader
    {
        private const uint SupportedVersion = 2;

        public static NavMeshRawData Load(string path)
        {
            return Load(File.ReadAllBytes(path));
        }

        public static NavMeshRawData Load(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes, false))
            using (var reader = new BinaryReader(stream, Encoding.ASCII, false))
            {
                var magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
                if (magic != "RNAV")
                    throw new InvalidDataException($"导航数据 magic 非法: {magic}");

                var version = reader.ReadUInt32();
                if (version < 1 || version > SupportedVersion)
                    throw new InvalidDataException($"不支持的导航数据版本: {version}");

                var data = new NavMeshRawData
                {
                    AgentHeight = reader.ReadSingle(),
                    AgentRadius = reader.ReadSingle(),
                    AgentMaxClimb = reader.ReadSingle(),
                    CellSize = reader.ReadSingle(),
                    CellHeight = reader.ReadSingle(),
                };

                var vertexCount = reader.ReadInt32();
                if (vertexCount < 0)
                    throw new InvalidDataException($"顶点数量非法: {vertexCount}");

                data.Vertices = new float[vertexCount * 3];
                for (var i = 0; i < data.Vertices.Length; i++)
                    data.Vertices[i] = reader.ReadSingle();

                var triangleCount = reader.ReadInt32();
                if (triangleCount < 0)
                    throw new InvalidDataException($"三角形数量非法: {triangleCount}");

                data.Indices = new int[triangleCount * 3];
                for (var i = 0; i < data.Indices.Length; i++)
                    data.Indices[i] = reader.ReadInt32();

                data.Areas = reader.ReadBytes(triangleCount);

                var linkCount = reader.ReadInt32();
                if (linkCount < 0)
                    throw new InvalidDataException($"Link 数量非法: {linkCount}");

                for (var i = 0; i < linkCount; i++)
                {
                    data.Links.Add(new NavMeshLinkData
                    {
                        StartPos = new[] { reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle() },
                        EndPos = new[] { reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle() },
                        Radius = reader.ReadSingle(),
                        Bidirectional = reader.ReadByte() != 0,
                        Area = reader.ReadByte(),
                    });
                }

                foreach (var index in data.Indices)
                {
                    if (index < 0 || index >= vertexCount)
                        throw new InvalidDataException($"三角形索引越界: {index}");
                }

                if (version >= 2)
                {
                    var gateCount = reader.ReadInt32();
                    if (gateCount < 0)
                        throw new InvalidDataException($"关卡门数量非法: {gateCount}");

                    for (var i = 0; i < gateCount; i++)
                    {
                        var gate = new NavMeshGateData
                        {
                            GateId = reader.ReadInt32(),
                            Center = new[] { reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle() },
                            Size = new[] { reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle() },
                            RotationY = reader.ReadSingle(),
                            Open = reader.ReadByte() != 0,
                        };

                        if (gate.GateId <= 0)
                            throw new InvalidDataException($"关卡门 gateID 非法: {gate.GateId}");
                        if (gate.Size[0] <= 0f || gate.Size[1] <= 0f || gate.Size[2] <= 0f)
                            throw new InvalidDataException($"关卡门 {gate.GateId} 尺寸非法");

                        data.Gates.Add(gate);
                    }
                }

                return data;
            }
        }
    }
}
