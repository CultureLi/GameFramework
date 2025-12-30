using Framework;
using GameEntry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;

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
            FW.UIMgr.OpenView("Role/UIRole");
        }
    }
}
