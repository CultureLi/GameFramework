using Framework;
using GameEntry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameMain.UI
{
    internal class UIPopup1 : ViewBase
    {
        public void OpenRole()
        {
            FW.UIMgr.OpenWnd("Role/UIRole");
        }

        public void OpenPopup2()
        {
            FW.UIMgr.OpenPopup("Popup/UIPopup2");
        }
    }
}
