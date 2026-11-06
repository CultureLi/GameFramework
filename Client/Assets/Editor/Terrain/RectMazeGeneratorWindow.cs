using UnityEditor;
using UnityEngine;

namespace GameFramework.EditorTools.TerrainGen
{
    public sealed class RectMazeGeneratorWindow : EditorWindow
    {
        [MenuItem("Tools/OpenWorld/Rect Maze Generator")]
        private static void Open()
        {
            var window = GetWindow<RectMazeGeneratorWindow>("Rect Maze Generator");
            window.minSize = new Vector2(440f, 380f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("矩形迷宫地形生成", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "目标场景: " + RectMazeGenerator.ScenePath + "\n\n" +
                "根据需求文档中的矩形草图生成：\n" +
                "  - 地形高度全部为 0，清除树和草，只保留地表\n" +
                "  - 红色围墙使用 AssetPathCfg 301 / Wall_1.prefab\n" +
                "  - Building 下 ConfigId 1~7 的关卡移动到绿色框位置\n" +
                "  - 关卡、建筑、采集物挂 NavMeshModifier 并勾选 Ignore From Build",
                MessageType.Info);

            EditorGUILayout.Space(8f);

            if (GUILayout.Button("生成地形 + 围墙 + 关卡位置", GUILayout.Height(36f)))
            {
                if (Confirm("会压平地形、重建围墙并移动 7 个关卡，继续？"))
                    RectMazeGenerator.Generate(false);
            }

            if (GUILayout.Button("生成并烘焙 NavMeshSurface", GUILayout.Height(36f)))
            {
                if (Confirm("会压平地形、重建围墙、移动 7 个关卡并重新烘焙 NavMesh，继续？"))
                    RectMazeGenerator.Generate(true);
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("局部操作", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("只重定位 7 个关卡", GUILayout.Height(28f)))
                {
                    if (Confirm("只移动 Building 下的 7 个关卡到绿色框位置？"))
                        RectMazeGenerator.RepositionGatesOnly();
                }

                if (GUILayout.Button("清除生成的 Walls", GUILayout.Height(28f)))
                {
                    if (Confirm("删除 DataEditor/Walls 下生成的围墙？"))
                        RectMazeGenerator.ClearGeneratedWallsOnly();
                }
            }

            EditorGUILayout.Space(4f);

            if (GUILayout.Button("给 Walls 墙体添加 MapEntityInfo (301)", GUILayout.Height(30f)))
            {
                if (Confirm("给 DataEditor/Walls 下已有墙体添加/更新 MapEntityInfo？\n\n" +
                            "不会重新生成地形和围墙；InstanceId 在导出时生成。"))
                    RectMazeGenerator.AddMapEntityInfoToWallsOnly();
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.HelpBox(
                "生成后还需要：\n" +
                "1. 打开地图编辑器重新导出客户端/服务端地图数据（关卡位置/缩放已变化）\n" +
                "2. 使用 Tools/RecastNavigationServer/Export NavMesh 重新导出 .rnav\n" +
                "3. 重启 GameServer 加载新的导航数据",
                MessageType.Warning);
        }

        private static bool Confirm(string message)
        {
            return EditorUtility.DisplayDialog("Rect Maze Generator", message, "继续", "取消");
        }
    }
}
