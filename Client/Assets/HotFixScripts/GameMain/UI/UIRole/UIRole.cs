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
        public void OpenMail()
        {
            FW.UIMgr.OpenUI("Mail/UIMail", (int)UIGroupType.Normal);
        }

        public void OpenPopup1()
        {
            FW.UIMgr.OpenUI("Popup/UIPopup1", (int)UIGroupType.Popup);
        }

        public void OpenPopup2()
        {
            FW.UIMgr.OpenUI("Popup/UIPopup2", (int)UIGroupType.Popup);
        }
    }
}
