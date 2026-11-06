using UnityEngine;

namespace GameMain
{
    /// <summary>
    /// 挂在编辑器摆放出来的地图实体上，记录它来自哪一条配置。
    /// 导出时只读取 <see cref="ConfigId"/>，资源路径通过配置表换算，场景里不直接引用 prefab。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapEntityInfo : MonoBehaviour
    {
        [Tooltip("实体类别，由所在目录决定。")]
        public MapEntityCategory Category;

        [Tooltip("MapBuildingCfg / AssetPathCfg(装饰) / MapResourceCfg 中的 Id。")]
        public int ConfigId;

        [Tooltip("地图实例唯一 ID，放置时生成，导出和服务端权限都使用它。")]
        public string InstanceId;

        [Tooltip("配置表换算出来的 AssetPathCfg.Id，仅用于编辑器查看和排查。")]
        public int AssetId;
    }
}
