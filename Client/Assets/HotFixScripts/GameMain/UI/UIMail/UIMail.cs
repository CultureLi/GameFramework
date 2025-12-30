using Framework;
using GameEntry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameMain.UI
{
    internal class UIMail : ViewBase
    {
        public void OnBack()
        {
            Close();
        }

        public void OpenRole()
        {
            FW.UIMgr.OpenView("Role/UIRole");
        }

        public void OpenPopup1()
        {
            FW.UIMgr.OpenPopup("Popup/UIPopup1");
        }

        public void OpenPopup2()
        {
            FW.UIMgr.OpenPopup("Popup/UIPopup2");
        }
    }
}
