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
            UIMgr.UIRoot.ShowBlurMask(true);
        }

        public override void OnAfterCloseUI(UIViewWrapper wrapper)
        {
            if (RefocusTopUI())
            {
                UIMgr.UIRoot.ShowBlurMask(true);
            }
            else
            {
                UIMgr.UIRoot.ShowBlurMask(false);
                UIMgr.RefocusTopUI(EUIGroupType.HUD);
            }
        }

        public override void CloseAll()
        {
            base.CloseAll();
            UIMgr.UIRoot.ShowBlurMask(false);
        }

        public override void HideAll()
        {
            base.HideAll();
            UIMgr.UIRoot.ShowBlurMask(false);
        }
    }
}

