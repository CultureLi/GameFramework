using System;
using UnityEngine;

namespace GameMain
{
    /// <summary>
    /// 单个地图实体的导出数据。
    /// 按需求文档只保存配置 Id、世界 XZ 坐标、碰撞体包围盒大小和绕 Y 的旋转。
    /// 世界高度 Y 不导出，运行时按地形高度采样。
    /// </summary>
    [Serializable]
    public sealed class MapItemData
    {
        /// <summary>对应 MapBuildingCfg / AssetPathCfg(装饰) / MapResourceCfg 的 Id。</summary>
        public int Id;

        /// <summary>地图实例唯一 ID，放置时生成，客户端和服务端保持一致。</summary>
        public string InstanceId;

        /// <summary>世界坐标（X/Z）。</summary>
        public Vector2 Pos;

        /// <summary>世界空间包围盒大小（X/Z），编辑器从 Collider 取。</summary>
        public Vector2 Size;

        /// <summary>绕 Y 轴旋转（度）。</summary>
        public float RotationY;
    }
}
