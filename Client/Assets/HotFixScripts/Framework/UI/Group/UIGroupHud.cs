using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// HUD界面组-显示主界面
    /// </summary>
    internal class UIGroupHud : UIGroupBase
    {
        public UIGroupHud(IUIManager uiMgr, EUIGroupType groupType, Transform root) : base(uiMgr, groupType, root)
        {

        }

        public override void OnBeforeOpenUI(UIViewWrapper wrapper)
        {
            
        }
    }
}

