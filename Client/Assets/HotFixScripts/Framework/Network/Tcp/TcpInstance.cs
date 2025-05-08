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

        public void Connect(string host, int port)
        {
            if (_connecter != null && _connecter.IsConnected)
            {
                return;
            }
            _connecter = new Connecter();
            _connecter.ConnectAsync(host, port);
            
        }

        public void Disconnect()
        {
        
        }

        public void SendMsg(IMessage msg)
        {
        
        }

        public void RegisterMsg(Type msgType, Action<IMessage> handler)
        {
        }

        public void UnregisterMsg(Type msgType, Action<IMessage> handler)
        {
        }

    }
}
