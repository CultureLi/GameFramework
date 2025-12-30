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
            FW.UIMgr.OpenView("Role/UIRole");
        }

        public void OpenPopup1()
        {
            FW.UIMgr.OpenPopup("Popup/UIPopup1");
        }
    }
}
