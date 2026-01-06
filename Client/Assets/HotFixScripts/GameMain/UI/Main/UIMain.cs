using Framework;
using GameEntry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static GameMain.Event.Event;

namespace GameMain.UI
{
    internal class UIMain : ViewBase
    {

        public void OnBtnGotoLoginClick()
        {
            GameEntryMgr.Entry();
        }

        public override void OnShow(bool isInitShow, ViewData data)
        {
            base.OnShow(isInitShow, data);
        }


        public void OpenHud2()
        {
            FW.UIMgr.OpenHud("Main/UIHUD2");
        }

        public void OpenRole()
        {
            FW.UIMgr.OpenWnd("Role/UIRole");
        }

        public void OpenPopup1()
        {
            FW.UIMgr.OpenPopup("Popup/UIPopup1");
        }

        public void ShowTips()
        {
            FW.EventMgr.Broadcast(CommonTipsEvent.Create(Time.deltaTime.ToString()));
        }
    }
}
