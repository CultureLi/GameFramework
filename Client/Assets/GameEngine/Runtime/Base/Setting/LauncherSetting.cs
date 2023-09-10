using GameEngine.Runtime.Base.Variable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GameEngine.Runtime.Base.Setting
{
    public class LauncherSetting:Variable<LauncherSetting>
    {
        public AssetLoadMode playMode = AssetLoadMode.OfflinePlayMode;
        public int test;
    }
}
