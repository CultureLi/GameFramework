using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Framework
{
    public partial class TcpInstance
    {

        public TcpInstance(string host, int port)
        {
            _host = host;
            _port = port;
            _dispatcher = new Dispatcher();
        }

        private string _host;
        private int _port;

        private Connecter _connecter;
        private Sender _sender;
        private Receiver _receiver;
        private Dispatcher _dispatcher;
        private Cryptor _cryptor;

        // 密钥交换完成前发送的消息先缓存在这里，握手成功后统一发出。
        private readonly Queue<IMessage> _pendingMsgs = new Queue<IMessage>();
        private readonly object _pendingLock = new object();


        public bool IsDisposed
        {
            private set; get;
        }

        public bool IsConnected => _connecter?.IsConnected ?? false;

        public bool IsReady => _sender != null;

        public TcpClient TCPClient => _connecter?.TCPClient ?? null;


        public async Task ConnectAsync(Action<NetworkConnectState> cb = null)
        {
            // 重连时先清理旧连接，避免旧的收发线程继续操作新连接。
            DisposeConnection();

            _connecter = new Connecter();

            lock (_pendingLock)
            {
                _pendingMsgs.Clear();
            }

            await _connecter.ConnectAsync(_host, _port, (state) =>
            {
                if (state == NetworkConnectState.Succeed || state == NetworkConnectState.Connected)
                {
                    _cryptor = new Cryptor(this, () =>
                    {
                        _sender = new Sender(_connecter, _cryptor);
                        _receiver = new Receiver(_connecter, _cryptor, _dispatcher);
                        UnityEngine.Debug.Log("[Network] 密钥交换完成，收发器已创建");
                        FlushPendingMsgs();
                    });
                }

                cb?.Invoke(state);
            });

        }

        private void DisposeConnection()
        {
            _sender?.Dispose();
            _receiver?.Dispose();
            _cryptor?.Dispose();
            _connecter?.Dispose();

            _sender = null;
            _receiver = null;
            _cryptor = null;
        }

        public void DisConnect()
        {
            _connecter?.Disconnect();
        }

        public void SendMsg(IMessage msg)
        {
            if (msg == null)
                return;

            var sender = _sender;
            if (sender != null)
            {
                sender.SendMsg(msg);
                return;
            }

            lock (_pendingLock)
            {
                _pendingMsgs.Enqueue(msg);
            }

            UnityEngine.Debug.Log($"[Network] 发送器未就绪，暂存消息 {msg.GetType().Name}");
        }

        private void FlushPendingMsgs()
        {
            var count = 0;
            while (true)
            {
                IMessage msg;
                lock (_pendingLock)
                {
                    if (_pendingMsgs.Count == 0)
                        break;

                    msg = _pendingMsgs.Dequeue();
                }

                _sender?.SendMsg(msg);
                count++;
            }

            if (count > 0)
            {
                UnityEngine.Debug.Log($"[Network] 已发送 {count} 条握手前暂存的消息");
            }
        }

        public void RegisterMsg<T>(Action<T> handler) where T : IMessage
        {
            _dispatcher?.RegisterMsg(handler);
        }

        public void UnregisterMsg<T>(Action<T> handler) where T : IMessage
        {
            _dispatcher?.UnregisterMsg(handler);
        }


        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            _dispatcher?.Update(elapseSeconds, realElapseSeconds);
            _cryptor?.Update(elapseSeconds, realElapseSeconds);
        }
        public void Dispose()
        {
            DisposeConnection();
            _dispatcher?.Dispose();
        }

    }
}
