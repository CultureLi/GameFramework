using Framework;
using GameEntry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameMain
{
    internal partial class UITools
    {
        public static void OpenHUD(string name, ViewData uiData = null)
        {
            FW.UIMgr.OpenUI(name, (int)UIGroupType.HUD, uiData);
        }

        public static void OpenUI(string name, ViewData uiData = null)
        {
            FW.UIMgr.OpenUI(name, (int)UIGroupType.Normal, uiData = null);
        }

        public static void OpenMsgBox(string name, ViewData uiData = null)
        {
            FW.UIMgr.OpenUI(name, (int)UIGroupType.MsgBox, uiData);
        }

        public static void OpenTips(string name, ViewData uiData = null)
        {
            FW.UIMgr.OpenUI(name, (int)UIGroupType.Tips, uiData);
        }
    }
}
