using Framework;
using GameEntry;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace GameMain.UI
{
    internal class UIPopup2 : ViewBase
    {
        public override bool CanBeReleased { get; set; } = false;

        public Image icon;
        public void OpenRole()
        {
            FW.UIMgr.OpenWnd("Role/UIRole");
        }

        public void OpenPopup1()
        {
            FW.UIMgr.OpenPopup("Popup/UIPopup1");
        }

        public void OpenMsgBox()
        {
            var uiData = ReferencePool.Acquire<GameEntryMsgBoxData>();
            uiData.title = "Title";
            uiData.content = "这是一个测试";
            uiData.callback = (flag) =>
            {
                Debug.Log($"MsgBox 回调 {flag}");
            };
            FW.UIMgr.OpenPopup("Common/MsgBox/UICommonMsgBox", uiData);
        }

        public void ChangeIcon()
        {
            icon.SetSprite("Icon_Buff_01");
        }
    }
}
