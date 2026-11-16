namespace GameMain
{
    /// <summary>
    /// 客户端地图建筑关卡定义，对应服务器 GameServer/Map/MapDataStore.cs 的 MapGateDefinition。
    /// InstanceId 来自 BuildingData.asset 的 MapItemData。
    /// </summary>
    public sealed class MapGateDefinition
    {
        public string InstanceId;
        public int ConfigId;
        public int GateIndex;
        public float PosX;
        public float PosZ;
        public float SizeX;
        public float SizeZ;
        public float RotationY;
    }
}
