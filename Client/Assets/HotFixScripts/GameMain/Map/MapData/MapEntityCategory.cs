namespace GameMain
{
    /// <summary>
    /// 地图编辑器里可摆放的实体类别。顺序与编辑器页签一一对应。
    /// </summary>
    public enum MapEntityCategory
    {
        Building = 0,
        Decorate = 1,
        Resource = 2,

        /// <summary>
        /// 障碍物：和装饰物一样直接用 AssetPathCfg 配置，但不挂 NavMeshModifier(ignoreFromBuild)，
        /// 烘焙 NavMesh 时会被当成阻挡。装饰物则会被忽略，不影响导航网格。
        /// </summary>
        Obstacle = 3,

        /// <summary>
        /// 可建造区域：不对应任何配置表，编辑器中拖拽画出绿色矩形；
        /// 导出矩形的 Pos/Size/RotationY，运行时据此生成内城的建造网格。
        /// </summary>
        BuildableArea = 4,
    }
}
