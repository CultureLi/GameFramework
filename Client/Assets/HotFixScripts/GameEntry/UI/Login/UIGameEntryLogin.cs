using Cysharp.Threading.Tasks;
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

        private bool _loggingIn;

        private void Awake()
        {
            btnLogin.AddSafeListener(OnBtnLoginClick);
        }

        void OnBtnLoginClick()
        {
            if (_loggingIn)
                return;

            var account = accountInput != null ? accountInput.text.Trim() : string.Empty;
            var password = passwordInput != null ? passwordInput.text : string.Empty;

            if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(password))
            {
                Debug.LogWarning("[Login] 账号和密码不能为空");
                return;
            }

            _loggingIn = true;
            if (btnLogin != null)
                btnLogin.interactable = false;

            LoginAsync(account, password).Forget();
        }

        private async UniTaskVoid LoginAsync(string account, string password)
        {
            await LoginSession.LoginAsync(account, password, (success, message) =>
            {
                _loggingIn = false;
                if (btnLogin != null)
                    btnLogin.interactable = true;

                if (!success)
                {
                    Debug.LogError($"[Login] {message}");
                    return;
                }

                EnterGameMain();
            });
        }

        private void EnterGameMain()
        {
            Close();
            FW.UIMgr.CloseUI("GameEntry/UIGameEntryProgress");
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
