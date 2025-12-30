using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 一级界面组- 活动主界面、角色界面等一级界面
    /// 打开界面时，会关闭上层Popup界面
    /// </summary>
    internal class UIGroupView : UIGroupBase
    {
        public UIGroupView(IUIManager uiMgr, EUIGroupType groupType, Transform root) : base(uiMgr, groupType, root)
        {

        }

        public override void OnBeforeOpenUI(UIViewWrapper wrapper)
        {
            // 隐藏主界面
            UIMgr.HideAll(EUIGroupType.HUD);
            // 隐藏view中其他界面
            UIMgr.HideAll(EUIGroupType.View);
            // 关闭弹窗
            UIMgr.CloseAll(EUIGroupType.Popup);
        }

        public override void OnAfterCloseUI(UIViewWrapper wrapper)
        {
            if (RefocusTopUI())
            {
                return;
            }
            else
            {
                UIMgr.RefocusTopUI(EUIGroupType.HUD);
            }
        }
    }
}

