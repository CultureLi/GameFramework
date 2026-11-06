using cfg;

namespace GameMain
{
    /// <summary>
    /// 配置表里的建筑类型判断。
    ///
    /// 当前 MapBuildingCfg.Type 的历史值是 1=关卡、2=普通建筑，
    /// 而 proto 的 BuildingType 枚举是 Base=1、Gate=2，两者数值相反。
    /// 这里同时看 Type 和资源路径/名称，兼容当前数据和后续按枚举整理过的数据。
    /// 正式数据最好统一成 BuildingType 枚举，届时可以删掉资源路径兜底。
    /// </summary>
    public static class MapBuildingTypeHelper
    {
        public static bool IsGate(MapBuildingCfg config, TbAssetPathCfg assetTable)
        {
            if (config == null)
                return false;

            var asset = assetTable?.Get(config.AssetId);
            string assetPath = asset?.AssetPath ?? string.Empty;
            bool gateLikeAsset = !string.IsNullOrEmpty(assetPath) &&
                                 assetPath.IndexOf("Gate", System.StringComparison.OrdinalIgnoreCase) >= 0;
            bool gateLikeName = !string.IsNullOrEmpty(config.Name) && config.Name.Contains("关卡");

            // 只有外观/名称确实像关卡，才用 Type 确认；避免当前数据里 Type=2 的普通建筑
            // 被错误地当成 BuildingType.Gate。
            if (!gateLikeAsset && !gateLikeName)
                return false;

            return config.Type == 1 ||
                   config.Type == (int)Google.Protobuf.WellKnownTypes.BuildingType.Gate;
        }
    }
}
