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
        Connecter _connecter;
        Sender _sender;
        Receiver _receiver;
        Dispatcher _dispatcher;

        Dictionary<Type, List<Delegate>> handlerMap = new Dictionary<Type, List<Delegate>>();

        public bool IsDisposed;

        public TcpInstance()
        {
            _dispatcher = new Dispatcher();
        }

        public void Connect(string host, int port)
        {
            if (_connecter != null && _connecter.IsConnected)
            {
                return;
            }
            _connecter = new Connecter();
            _connecter.onConnectResult = OnConnectResult;
            _connecter.ConnectAsync(host, port);
            
        }

        private void OnConnectResult(NetworkConnectState state)
        {
            if (state == NetworkConnectState.Succeed)
            {
                _sender = new Sender(_connecter);
                _receiver = new Receiver(_connecter, _dispatcher);
            }
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
        }
        public void Dispose()
        {
            _connecter?.Dispose();
            _sender?.Dispose();
            _receiver?.Dispose();
            _dispatcher?.Dispose();
        }

    }
}
