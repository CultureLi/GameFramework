using UnityEngine;

namespace GameFramework.Level
{
    /// <summary>
    /// 调试用：按 G 一键切换场景里所有关卡门的开关，方便观察 NavMeshObstacle 的挖洞效果。
    /// </summary>
    public sealed class MazeGateDebugToggle : MonoBehaviour
    {
        public KeyCode toggleKey = KeyCode.G;

        private void Update()
        {
            if (!Input.GetKeyDown(toggleKey))
                return;

            var gates = FindObjectsOfType<LevelGate>(true);
            if (gates.Length == 0)
                return;

            bool open = !gates[0].IsOpen;
            foreach (var gate in gates)
                gate.RequestSetOpen(open);

            Debug.Log($"[MazeGate] 所有关卡门已{(open ? "打开" : "关闭")}，共 {gates.Length} 个");
        }
    }
}
