using UnityEngine;

namespace GameMain
{
    /// <summary>
    /// 场景中所有可建造区域（内城手动放置建筑的范围）的矩形数据。
    /// Id 固定为 0（不关联配置表），只用 Pos/Size/RotationY 描述矩形。
    /// </summary>
    [CreateAssetMenu(menuName = "Map/BuildableAreaData", fileName = "BuildableAreaData")]
    public sealed class BuildableAreaData : MapModuleData
    {
    }
}
