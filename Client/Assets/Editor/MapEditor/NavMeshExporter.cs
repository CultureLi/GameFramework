using System.Collections.Generic;
using System.IO;
using System.Text;
using GameFramework.Level;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace RecastNavigation
{
    // 把当前已烘焙的 NavMesh(CalculateTriangulation)+ 场景里的 NavMeshLink 导出成 .rnav 二进制文件,
    // 格式定义见 E:/Work/RecastNavigationServer/DATA_FORMAT.md,供 RecastNavigationServer PoC 读取验证。
    public static class NavMeshExporter
    {
        private const string Magic = "RNAV";
        private const uint FormatVersion = 2;

        private struct LinkExportData
        {
            public Vector3 startPos;
            public Vector3 endPos;
            public float radius;
            public bool bidirectional;
            public byte area;
        }

        private struct GateExportData
        {
            public int gateId;
            public Vector3 center;
            public Vector3 size;
            public float rotationY;
            public bool open;
        }

        //[MenuItem("Tools/RecastNavigationServer/Export NavMesh")]
        public static void ExportActiveScene()
        {
            string path = EditorUtility.SaveFilePanel("Export NavMesh (.rnav)", Application.dataPath, "navmesh", "rnav");
            if (string.IsNullOrEmpty(path))
                return;

            Export(path);
        }

        /// <summary>
        /// 服务端实际读取的 .rnav 路径：GameServer.csproj 把 Server/NavData/navmesh.rnav 链接进了
        /// 各个构建输出，所以这里直接固定到仓库里的这一份，而不是跟当前打开的场景走。
        /// </summary>
        public static string GetServerNavDataPath()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Server", "NavData", "navmesh.rnav"));
        }

        /// <summary>给 MapEditor 窗口用的导出入口：直接写到 Server/NavData/navmesh.rnav。</summary>
        public static bool ExportToServerNavData()
        {
            return Export(GetServerNavDataPath());
        }

        private static bool Export(string path)
        {
            NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
            if (tri.vertices.Length == 0 || tri.indices.Length == 0)
            {
                Debug.LogError("[NavMeshExporter] NavMesh.CalculateTriangulation() 返回空数据,请先在场景里 Bake NavMesh。");
                return false;
            }

            // Agent 参数(agentHeight/agentRadius/agentClimb)和体素量化步长取自场景里第一个 NavMeshSurface 的 agentTypeID。
            // 多个 NavMeshSurface 用了不同 agentTypeID 的情况,本次导出不处理(PoC 只验证单一 agent 类型)。
            int agentTypeID = 0;
            var surfaces = Object.FindObjectsOfType<NavMeshSurface>();
            if (surfaces.Length > 0)
            {
                agentTypeID = surfaces[0].agentTypeID;
                if (surfaces.Length > 1)
                {
                    Debug.LogWarning($"[NavMeshExporter] 场景中有 {surfaces.Length} 个 NavMeshSurface,可能对应不同 agentTypeID;本次导出只使用第一个(agentTypeID={agentTypeID})的烘焙参数。");
                }
            }
            else
            {
                Debug.LogWarning("[NavMeshExporter] 场景中没有找到 NavMeshSurface,agentTypeID 使用默认值 0。");
            }

            NavMeshBuildSettings buildSettings = NavMesh.GetSettingsByID(agentTypeID);

            // cellSize/cellHeight 只用于 .rnav 里顶点的体素量化步长(dtNavMeshCreateParams 的 cs/ch),
            // 和 Unity 实际烘焙时 Recast 体素化用的步长没有强制对应关系,这里按 Unity 的默认公式(agentRadius/3)估算,
            // 除非 NavMeshSurface 显式 override 了 voxelSize。
            float cellSize = buildSettings.overrideVoxelSize ? buildSettings.voxelSize : buildSettings.agentRadius / 3f;
            float cellHeight = cellSize;

            List<LinkExportData> links = CollectLinks();
            List<GateExportData> gates = CollectGates();

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            WriteFile(path, tri, buildSettings, cellSize, cellHeight, links, gates);

            Debug.Log($"[NavMeshExporter] 导出完成: {tri.vertices.Length} 个顶点, {tri.indices.Length / 3} 个三角形, {links.Count} 个 NavMeshLink, {gates.Count} 个动态关卡门 -> {path}");
            return true;
        }

        private static List<LinkExportData> CollectLinks()
        {
            var result = new List<LinkExportData>();
            var navLinks = Object.FindObjectsOfType<NavMeshLink>();
            foreach (var link in navLinks)
            {
                if (!link.isActiveAndEnabled)
                    continue;

                // NavMeshLink.startPoint/endPoint 是相对自身 transform 的局部坐标(Unity 内部通过
                // NavMesh.AddLink(link, transform.position, transform.rotation) 换算成世界坐标),
                // 这里用 TransformPoint 还原世界坐标,写入 .rnav 的 startPos/endPos。
                Transform t = link.transform;
                Vector3 startWorld = t.TransformPoint(link.startPoint);
                Vector3 endWorld = t.TransformPoint(link.endPoint);

                result.Add(new LinkExportData
                {
                    startPos = startWorld,
                    endPos = endWorld,
                    // NavMeshLink 暴露的是 width(两侧总宽度),.rnav 格式里是 radius,取一半近似。
                    radius = link.width * 0.5f,
                    bidirectional = link.bidirectional,
                    area = (byte)link.area,
                });
            }
            return result;
        }

        /// <summary>
        /// 导出动态关卡门（LevelGate）。门的 BoxCollider / NavMeshObstacle 就是服务端需要跳过的区域。
        /// 服务端加载后会按 gateId 把门覆盖的导航多边形标记为不可通行。
        /// </summary>
        private static List<GateExportData> CollectGates()
        {
            var result = new List<GateExportData>();
            var seenIds = new HashSet<int>();
            var gates = Object.FindObjectsOfType<LevelGate>(true);

            foreach (var gate in gates)
            {
                if (gate.gateID <= 0)
                {
                    Debug.LogWarning($"[NavMeshExporter] 关卡门 {gate.name} 没有设置 gateID，已跳过。");
                    continue;
                }

                if (!seenIds.Add(gate.gateID))
                {
                    Debug.LogError($"[NavMeshExporter] 关卡门 gateID 重复: {gate.gateID}（{gate.name}），已跳过。");
                    continue;
                }

                Vector3 center;
                Vector3 size;

                if (gate.obstacle != null)
                {
                    center = gate.transform.TransformPoint(gate.obstacle.center);
                    size = Vector3.Scale(gate.obstacle.size, gate.transform.lossyScale);
                }
                else if (gate.blockingCollider is BoxCollider box)
                {
                    center = gate.transform.TransformPoint(box.center);
                    size = Vector3.Scale(box.size, gate.transform.lossyScale);
                }
                else if (gate.blockingCollider != null)
                {
                    center = gate.blockingCollider.bounds.center;
                    size = gate.blockingCollider.bounds.size;
                }
                else
                {
                    Debug.LogWarning($"[NavMeshExporter] 关卡门 {gate.name} 没有 Collider/Obstacle，已跳过。");
                    continue;
                }

                result.Add(new GateExportData
                {
                    gateId = gate.gateID,
                    center = center,
                    size = size,
                    rotationY = gate.transform.eulerAngles.y,
                    open = gate.IsOpen,
                });
            }

            return result;
        }

        private static void WriteFile(string path, NavMeshTriangulation tri, NavMeshBuildSettings settings,
            float cellSize, float cellHeight, List<LinkExportData> links, List<GateExportData> gates)
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Encoding.ASCII.GetBytes(Magic));
                writer.Write(FormatVersion);

                writer.Write(settings.agentHeight);
                writer.Write(settings.agentRadius);
                writer.Write(settings.agentClimb);
                writer.Write(cellSize);
                writer.Write(cellHeight);

                writer.Write(tri.vertices.Length);
                foreach (Vector3 v in tri.vertices)
                {
                    writer.Write(v.x);
                    writer.Write(v.y);
                    writer.Write(v.z);
                }

                int triangleCount = tri.indices.Length / 3;
                writer.Write(triangleCount);
                foreach (int idx in tri.indices)
                {
                    writer.Write(idx);
                }

                foreach (int area in tri.areas)
                {
                    writer.Write((byte)area);
                }

                writer.Write(links.Count);
                foreach (LinkExportData link in links)
                {
                    writer.Write(link.startPos.x);
                    writer.Write(link.startPos.y);
                    writer.Write(link.startPos.z);
                    writer.Write(link.endPos.x);
                    writer.Write(link.endPos.y);
                    writer.Write(link.endPos.z);
                    writer.Write(link.radius);
                    writer.Write((byte)(link.bidirectional ? 1 : 0));
                    writer.Write(link.area);
                }

                // v2: 动态关卡门
                writer.Write(gates.Count);
                foreach (GateExportData gate in gates)
                {
                    writer.Write(gate.gateId);
                    writer.Write(gate.center.x);
                    writer.Write(gate.center.y);
                    writer.Write(gate.center.z);
                    writer.Write(gate.size.x);
                    writer.Write(gate.size.y);
                    writer.Write(gate.size.z);
                    writer.Write(gate.rotationY);
                    writer.Write((byte)(gate.open ? 1 : 0));
                }
            }
        }
    }
}
