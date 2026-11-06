using System.Collections.Generic;
using UnityEngine;

namespace GameMain
{
    /// <summary>
    /// 建筑物 / 装饰物 / 采集资源导出文件的公共基类。
    /// 每个模块一个 ScriptableObject 资产，例如 BuildingData.asset。
    /// </summary>
    public abstract class MapModuleData : ScriptableObject
    {
        public List<MapItemData> Items = new List<MapItemData>();
    }
}
