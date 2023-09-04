using GameEngine.Runtime.Base;
using GameEngine.Runtime.Base.Variable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.GameEngine.Runtime.Base.Setting
{
    public class LauncherSetting:Variable<LauncherSetting>
    {
        public AssetLoadMode playMode = AssetLoadMode.EditorSimulateMode;
        public int test;
    }
}
