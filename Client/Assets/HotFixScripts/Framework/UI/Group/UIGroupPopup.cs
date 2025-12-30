using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 弹窗界面组-弹窗界面（二级界面 / 消息框 / MsgBox)
    /// </summary>
    internal class UIGroupPopup : UIGroupBase
    {
        public UIGroupPopup(IUIManager uiMgr, EUIGroupType groupType, Transform root) : base(uiMgr, groupType, root)
        {

        }

        public override void OnBeforeOpenUI(UIViewWrapper wrapper)
        {

        }
    }
}

