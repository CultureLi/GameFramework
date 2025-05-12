using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

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


        public bool IsDisposed
        {
            private set; get;
        }

        public bool IsConnected => _connecter?.IsConnected ?? false;

        public TcpClient TCPClient => _connecter?.TCPClient ?? null;


        public void ConnectAsync(Action<NetworkConnectState> cb = null)
        {
            if (_connecter != null && _connecter.IsConnected)
            {
                return;
            }
            _connecter = new Connecter();
            _connecter.ConnectAsync(_host, _port, (state) =>
            {
                if (state == NetworkConnectState.Succeed)
                {
                    _cryptor = new Cryptor(this, () =>
                    {
                        _sender = new Sender(_connecter, _cryptor);
                        _receiver = new Receiver(_connecter, _cryptor, _dispatcher);
                    });
                }

                cb?.Invoke(state);
            });

        }

        public void DisConnect()
        {
            _connecter.Disconnect();
        }

        public void SendMsg(IMessage msg)
        {
            _sender?.SendMsg(msg);
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
            _receiver?.Update(elapseSeconds, realElapseSeconds);
            _cryptor?.Update(elapseSeconds, realElapseSeconds);
        }
        public void Dispose()
        {
            _connecter?.Dispose();
            _sender?.Dispose();
            _receiver?.Dispose();
            _dispatcher?.Dispose();
            _cryptor?.Dispose();
        }

    }
}
