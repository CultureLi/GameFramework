using System;
using Cysharp.Threading.Tasks;
using Framework;
using Google.Protobuf.WellKnownTypes;
using UnityEngine;

namespace GameEntry
{
    /// <summary>
    /// 登录会话。
    /// 负责：连接服务器 -> 发送 LoginReq -> 等待 LoginAck -> 登录成功后维持心跳。
    /// 网络底层完全复用 Framework.INetworkMgr（TCP + RSA/AES + LZ4 + protobuf）。
    /// </summary>
    public static class LoginSession
    {
        private const float kHeartbeatInterval = 5f;
        private const float kLoginTimeout = 10f;

        private static bool _handlersRegistered;
        private static bool _loginPending;
        private static bool _loggedIn;
        private static int _loginTimeoutTimerId;
        private static int _heartbeatTimerId;
        private static Action<bool, string> _loginCallback;

        public static bool LoggedIn => _loggedIn;

        public static ulong PlayerId { get; private set; }

        /// <summary>
        /// 服务端下发的出生点（脚底坐标）。
        /// </summary>
        public static Vector3 SpawnPosition { get; private set; }

        public static float SpawnRotationY { get; private set; }

        public static async UniTask LoginAsync(string account, string password, Action<bool, string> callback)
        {
            if (_loginPending || _loggedIn)
                return;

            _loginPending = true;
            _loginCallback = callback;

            AppConfig.GetLoginServer(out var host, out var port);

            // 兜底：如果 Entry 阶段 Protos 程序集还没加载，这里强制加载并重新注册消息
            if (ProtoTypeHelper.GetMsgId(typeof(LoginReq)) == 0)
            {
                _ = typeof(LoginReq).Assembly;
                ProtoTypeHelper.CollectMsgType();
            }

            var loginMsgId = ProtoTypeHelper.GetMsgId(typeof(LoginReq));
            if (loginMsgId == 0)
            {
                Complete(false, "LoginReq 未注册到 ProtoTypeHelper，请检查 Protos 程序集是否加载");
                return;
            }

            Debug.Log($"[Login] 连接 {host}:{port}，LoginReq msgId={loginMsgId}");

            FW.NetMgr.Create(host, port);

            if (!_handlersRegistered)
            {
                FW.NetMgr.RegisterMsg<LoginAck>(OnLoginAck);
                FW.NetMgr.RegisterMsg<HeartBeatAck>(OnHeartBeatAck);
                _handlersRegistered = true;
            }

            var connectState = NetworkConnectState.Failed;
            await FW.NetMgr.ConnectAsync(NetChannelType.Main, state => connectState = state);

            if (connectState != NetworkConnectState.Succeed && connectState != NetworkConnectState.Connected)
            {
                Complete(false, "连接服务器失败");
                return;
            }

            Debug.Log($"[Login] TCP 已连接 {host}:{port}，等待密钥交换并发送 LoginReq");

            // 此时 RSA/AES 密钥交换可能还没完成，TcpInstance 会先缓存消息，
            // 等握手结束后自动发送。
            FW.NetMgr.SendMsg(new LoginReq
            {
                Account = account,
                Password = password,
            });

            Debug.Log("[Login] LoginReq 已加入发送队列");

            _loginTimeoutTimerId = FW.TimerMgr.AddTimer(kLoginTimeout, _ =>
            {
                _loginTimeoutTimerId = 0;
                Complete(false, "登录超时");
            });
        }

        private static void OnLoginAck(LoginAck ack)
        {
            Debug.Log($"[Login] 收到 LoginAck code={ack.Code} message={ack.Message}");

            if (!_loginPending)
            {
                Debug.LogWarning("[Login] 登录已超时或取消，忽略 LoginAck");
                return;
            }

            if (ack.Code != 0)
            {
                Complete(false, string.IsNullOrEmpty(ack.Message) ? $"登录失败({ack.Code})" : ack.Message);
                return;
            }

            _loggedIn = true;
            PlayerId = ack.PlayerId;

            if (ack.SpawnPos != null)
            {
                SpawnPosition = new Vector3(ack.SpawnPos.X, ack.SpawnPos.Y, ack.SpawnPos.Z);
            }
            SpawnRotationY = ack.SpawnRotationY;

            StartHeartbeat();

            Debug.Log($"[Login] 登录成功 playerId={ack.PlayerId}");
            Complete(true, ack.Message);
        }

        private static void OnHeartBeatAck(HeartBeatAck ack)
        {
            // 目前只需要靠心跳维持连接，服务端的超时检测在服务端完成。
            Debug.Log($"[Heartbeat] rtt≈{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - ack.ClientTime}ms");
        }

        private static void StartHeartbeat()
        {
            if (_heartbeatTimerId != 0)
                return;

            // duration < 0 表示无限循环。
            _heartbeatTimerId = FW.TimerMgr.AddTimer(kHeartbeatInterval, _ =>
            {
                if (!FW.NetMgr.IsConnected())
                {
                    StopHeartbeat();
                    return;
                }

                FW.NetMgr.SendMsg(new HeartBeatReq
                {
                    ClientTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                });
            }, -1f);
        }

        public static void StopHeartbeat()
        {
            if (_heartbeatTimerId == 0)
                return;

            FW.TimerMgr.RemoveTimer(_heartbeatTimerId);
            _heartbeatTimerId = 0;
        }

        private static void Complete(bool success, string message)
        {
            if (!_loginPending)
                return;

            _loginPending = false;

            if (_loginTimeoutTimerId != 0)
            {
                FW.TimerMgr.RemoveTimer(_loginTimeoutTimerId);
                _loginTimeoutTimerId = 0;
            }

            var callback = _loginCallback;
            _loginCallback = null;
            callback?.Invoke(success, message);
        }
    }
}
