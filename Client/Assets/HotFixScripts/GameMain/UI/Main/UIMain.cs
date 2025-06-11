using Framework;
using GameEntry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;

namespace GameMain
{
    internal class UIMain : ViewBase
    {
        public Button btnGotoLogin;

        private void Awake()
        {
            btnGotoLogin.onClick.AddListener(OnBtnGotoLoginClick);
        }

        void OnBtnGotoLoginClick()
        {
            GameEntryMgr.Entry();
        }

        public override void OnShow(bool isInitShow, ViewData data)
        {
            base.OnShow(isInitShow, data);
        }

        public override void OnClose()
        {
            base.OnClose();
        }
    }
}
