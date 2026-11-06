using UnityEngine;

namespace GameMain
{
    /// <summary>
    /// 场景中所有障碍物（AssetPathCfg）的摆放数据。
    /// 和 DecorateData 一样直接用 AssetPathCfg，区别在编辑器里放置时是否挂 NavMeshModifier：
    /// 障碍物不挂，烘焙 NavMesh 时会被当成阻挡。
    /// </summary>
    [CreateAssetMenu(menuName = "Map/ObstacleData", fileName = "ObstacleData")]
    public sealed class ObstacleData : MapModuleData
    {
    }
}
