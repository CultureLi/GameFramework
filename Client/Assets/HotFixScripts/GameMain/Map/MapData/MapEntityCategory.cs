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
    }
}
