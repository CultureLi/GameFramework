using Cysharp.Threading.Tasks;
using Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameEntry
{
    internal class UIGameEntryLogin : ViewBase
    {
        public TMP_Dropdown dropDown;
        public TMP_InputField accountInput;
        public TMP_InputField passwordInput;
        public Button btnLogin;
        public Button btnLaunchServer;

        private bool _loggingIn;

        private void Awake()
        {
            btnLogin.AddSafeListener(OnBtnLoginClick);
            if (btnLaunchServer != null)
                btnLaunchServer.AddSafeListener(OnBtnLaunchServerClick);
            dropDown.onValueChanged.AddListener(OnDropDownValueChanged);
        }

        void OnDropDownValueChanged(int idx)
        {
            GameModeSettings.Mode = idx == 0 ? GamePlayMode.SinglePlayer : GamePlayMode.Online;
            UpdateInputState();
        }

        /// <summary>单机模式不需要账号密码，禁用输入框避免误解。</summary>
        private void UpdateInputState()
        {
            bool singlePlayer = GameModeSettings.IsSinglePlayer;
            if (accountInput != null)
                accountInput.interactable = !singlePlayer;
            if (passwordInput != null)
                passwordInput.interactable = !singlePlayer;

            if (btnLaunchServer != null)
                btnLaunchServer.gameObject.SetActive(!singlePlayer);
        }

        /// <summary>
        /// 启动服务器：先探测登录端口，已经开着就提示；否则调用 Server/run.bat 启动。
        /// </summary>
        void OnBtnLaunchServerClick()
        {
            AppConfig.GetLoginServer(out var host, out var port);

            if (IsServerRunning(host, port))
            {
                Debug.Log($"[Login] 服务器已开启 {host}:{port}");
                ShowCommonTips("服务器已开启");
                return;
            }

            if (TryLaunchServer())
                Debug.Log("[Login] 服务器未开启，已执行 Server/run.bat 启动");
            else
                Debug.LogError("[Login] 启动服务器失败，请检查 Server/run.bat 是否存在");
        }

        /// <summary>用 TCP 连接探测登录端口，避免重复启动服务器。</summary>
        private static bool IsServerRunning(string host, int port)
        {
            if (string.IsNullOrEmpty(host))
                host = "127.0.0.1";
            if (host == "0.0.0.0" || host == "::")
                host = "127.0.0.1";

            try
            {
                using (var client = new TcpClient())
                {
                    var asyncResult = client.BeginConnect(host, port, null, null);
                    bool connected = asyncResult.AsyncWaitHandle.WaitOne(500);
                    if (!connected || !client.Connected)
                        return false;

                    client.EndConnect(asyncResult);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Login] 探测服务器 {host}:{port} 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>用 cmd start 打开一个新窗口执行 Server/run.bat。</summary>
        private static bool TryLaunchServer()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor &&
                Application.platform != RuntimePlatform.WindowsPlayer)
            {
                Debug.LogWarning("[Login] 服务器启动脚本仅支持 Windows");
                return false;
            }

            string batPath = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "..", "Server", "run.bat"));
            if (!File.Exists(batPath))
            {
                batPath = Path.GetFullPath(
                    Path.Combine(Directory.GetCurrentDirectory(), "Server", "run.bat"));
            }

            if (!File.Exists(batPath))
            {
                Debug.LogError("[Login] 找不到服务器启动脚本: " + batPath);
                return false;
            }

            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c start \"GameServer\" \"" + batPath + "\"",
                    WorkingDirectory = Path.GetDirectoryName(batPath) ?? string.Empty,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                };

                System.Diagnostics.Process.Start(startInfo);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[Login] 执行服务器启动脚本失败: " + ex);
                return false;
            }
        }

        /// <summary>
        /// GameEntry 程序集不能直接引用 GameMain 的 UIDelegate，这里用反射调用，
        /// 与 GameEntryMgr.EnterGameMain 的方式保持一致。
        /// </summary>
        private static void ShowCommonTips(string message)
        {
            try
            {
                var type = Type.GetType("GameMain.UI.UIDelegate, GameMain");
                if (type == null)
                {
                    Debug.Log("[Login] " + message);
                    return;
                }

                const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
                var instanceProperty = type.GetProperty("I", flags)
                                       ?? type.BaseType?.GetProperty("I", flags);
                var instance = instanceProperty?.GetValue(null);
                var method = type.GetMethod("ShowCommonTips", BindingFlags.Public | BindingFlags.Instance,
                    null, new[] { typeof(string) }, null);

                if (instance != null && method != null)
                    method.Invoke(instance, new object[] { message });
                else
                    Debug.Log("[Login] " + message);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Login] 调用 UIDelegate.ShowCommonTips 失败: " + ex.Message);
            }
        }

        void OnBtnLoginClick()
        {
            if (_loggingIn)
                return;

            // 单机模式不连接服务器，也不需要账号密码，直接进入 Main。
            if (GameModeSettings.IsSinglePlayer)
            {
                _loggingIn = true;
                Debug.Log("[Login] 单机模式，跳过网络连接");
                EnterGameMain();
                return;
            }

            Debug.Log("[Login] 联机模式，开始连接服务器");

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

            // dropDown.value 默认 0（单机）；打开界面时同步一次全局模式。
            if (dropDown != null)
                GameModeSettings.Mode = dropDown.value == 1
                    ? GamePlayMode.Online
                    : GamePlayMode.SinglePlayer;

            Debug.Log($"[Login] 登录界面模式={GameModeSettings.Mode} " +
                      $"dropdown={(dropDown != null ? dropDown.value : -1)}");

            UpdateInputState();
        }

        public override void OnClose()
        {
            base.OnClose();
        }
    }
}
