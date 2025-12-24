using Framework;
using GameEntry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameMain.UI
{
    internal class UIPopup2 : ViewBase
    {
        public void OpenRole()
        {
            FW.UIMgr.OpenUI("Role/UIRole", (int)UIGroupType.Normal);
        }

        public void OpenPopup1()
        {
            FW.UIMgr.OpenUI("Popup/UIPopup1", (int)UIGroupType.Popup);
        }
    }
}
