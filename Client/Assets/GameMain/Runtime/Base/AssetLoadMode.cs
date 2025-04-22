using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameMain.Runtime.Base
{
    /// <summary>
    /// 资源启动模式，对应YooAsset 中的 EPlayMode
    /// </summary>
    public enum AssetLoadMode
    {
        //编辑器模式，不打bundle
        EditorSimulateMode,
        //单机模式
        OfflinePlayMode,
        //热更模式
        HostPlayMode
    }
}
