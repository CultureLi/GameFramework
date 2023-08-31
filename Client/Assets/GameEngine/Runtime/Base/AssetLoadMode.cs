using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameEngine.Runtime.Base
{
    public enum AssetLoadMode
    {
        //编辑器模式，不打bundle
        Editor,
        //单机模式
        Single,
        //热更模式
        HotUpdate,
    }
}
