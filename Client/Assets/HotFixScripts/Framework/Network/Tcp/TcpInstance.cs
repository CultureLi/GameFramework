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

        Dictionary<Type, List<Delegate>> handlerMap = new Dictionary<Type, List<Delegate>>();

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
                _receiver = new Receiver(_connecter);
            }
        }

        public void SendMsg(IMessage msg)
        {
            _sender?.SendMsg(msg);
        }

        public void RegisterMsg<T>(Action<T> handler) where T : IMessage
        {
            var msgType = typeof(T);
            if (!handlerMap.TryGetValue(msgType, out var list))
            {
                list = new List<Delegate>();
                handlerMap[msgType] = list;
            }
            if (list.Contains(handler))
                list.Add(handler);
        }

        public void UnregisterMsg(Type msgType, Action<IMessage> handler)
        {
        }

    }
}
