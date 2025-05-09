using Google.Protobuf;
using System;
using System.Collections.Generic;

namespace Framework
{
    /// <summary>
    /// 网络管理器接口。
    /// </summary>
    public interface INetworkMgr
    {
        void Create(string host, int port, NetChannelType type = NetChannelType.Main);
        void Connect(NetChannelType type = NetChannelType.Main);
        void Disconnect(NetChannelType type = NetChannelType.Main);
        void SendMsg(IMessage msg, NetChannelType type = NetChannelType.Main);
        void RegisterMsg<T>(Action<T> handler, NetChannelType type = NetChannelType.Main) where T : IMessage;
        void UnregisterMsg<T>(Action<T> handler, NetChannelType type = NetChannelType.Main) where T : IMessage;
    }
}
