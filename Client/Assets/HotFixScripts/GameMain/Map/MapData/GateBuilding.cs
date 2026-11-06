using GameFramework.Level;
using UnityEngine;

namespace GameMain
{
    /// <summary>
    /// 地图建筑关卡的运行时标记。MapDataStreaming 生成关卡建筑时挂上，
    /// 保存地图导出的 InstanceId，用于点击打开关卡信息、申请通行和归属管理。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GateBuilding : MonoBehaviour
    {
        public string InstanceId;
        public int ConfigId;

        public void Initialize(string instanceId, int configId)
        {
            InstanceId = instanceId;
            ConfigId = configId;

            // 建筑关卡是否可通行由服务端按玩家计算，客户端不再用全局 NavMeshObstacle 挖洞。
            var levelGate = GetComponent<LevelGate>();
            if (levelGate != null)
            {
                if (levelGate.obstacle != null)
                    levelGate.obstacle.enabled = false;

                levelGate.enabled = false;
            }
        }
    }
}
