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
    public class UIRole : ViewBase
    {
        public void OnBack()
        {
            Close();
        }
        public void OpenMail()
        {
            FW.UIMgr.OpenWnd("Mail/UIMail");
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
