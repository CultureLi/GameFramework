using Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameEntry
{
    internal class UIGameEntryLogin : ViewBase
    {
        public TMP_InputField accountInput;
        public TMP_InputField passwordInput;
        public Button btnLogin;

        private void Awake()
        {
            btnLogin.AddSafeListener(OnBtnLoginClick);
        }

        void OnBtnLoginClick()
        {
            Close();
            GameEntryMgr.I.EnterGameMain();
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
