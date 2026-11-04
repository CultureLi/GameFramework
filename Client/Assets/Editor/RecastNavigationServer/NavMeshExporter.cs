using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace RecastNavigationServer
{
    // 把当前已烘焙的 NavMesh(CalculateTriangulation)+ 场景里的 NavMeshLink 导出成 .rnav 二进制文件,
    // 格式定义见 E:/Work/RecastNavigationServer/DATA_FORMAT.md,供 RecastNavigationServer PoC 读取验证。
    public static class NavMeshExporter
    {
        private const string Magic = "RNAV";
        private const uint FormatVersion = 1;

        private struct LinkExportData
        {
            public Vector3 startPos;
            public Vector3 endPos;
            public float radius;
            public bool bidirectional;
            public byte area;
        }

        [MenuItem("Tools/RecastNavigationServer/Export NavMesh")]
        public static void ExportActiveScene()
        {
            string path = EditorUtility.SaveFilePanel("Export NavMesh (.rnav)", Application.dataPath, "navmesh", "rnav");
            if (string.IsNullOrEmpty(path))
                return;

            Export(path);
        }

        private static void Export(string path)
        {
            NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
            if (tri.vertices.Length == 0 || tri.indices.Length == 0)
            {
                Debug.LogError("[NavMeshExporter] NavMesh.CalculateTriangulation() 返回空数据,请先在场景里 Bake NavMesh。");
                return;
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

            WriteFile(path, tri, buildSettings, cellSize, cellHeight, links);

            Debug.Log($"[NavMeshExporter] 导出完成: {tri.vertices.Length} 个顶点, {tri.indices.Length / 3} 个三角形, {links.Count} 个 NavMeshLink -> {path}");
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

        private static void WriteFile(string path, NavMeshTriangulation tri, NavMeshBuildSettings settings,
            float cellSize, float cellHeight, List<LinkExportData> links)
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
            }
        }
    }
}
