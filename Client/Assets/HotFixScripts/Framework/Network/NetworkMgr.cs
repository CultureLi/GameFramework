using Google.Protobuf;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 网络管理器。
    /// </summary>
    internal sealed partial class NetworkMgr : INetworkMgr, IFramework
    {
        Dictionary<NetChannelType, TcpInstance> _tcpInstances = new Dictionary<NetChannelType, TcpInstance>();

        /// <summary>
        /// 初始化网络管理器的新实例。
        /// </summary>
        public NetworkMgr()
        {
            MsgTypeIdUtility.Init();
        }

        public void Create(string host, int port, NetChannelType type = NetChannelType.Main)
        {
            if (!_tcpInstances.ContainsKey(type))
            {
                var instance = new TcpInstance(host, port);
                _tcpInstances.Add(type, instance);
            }
        }

        public void Connect(NetChannelType type = NetChannelType.Main)
        {
            if (_tcpInstances.TryGetValue(type, out var instance))
            {
                instance.Connect();
            }
        }

        public void Disconnect(NetChannelType type = NetChannelType.Main)
        {
            if (_tcpInstances.TryGetValue(type, out var instance))
            {
                instance.DisConnect();
            }
        }

        public void SendMsg(IMessage msg, NetChannelType type = NetChannelType.Main)
        {
            if (_tcpInstances.TryGetValue(type, out var instance))
            {
                instance.SendMsg(msg);
            }
        }

        public void RegisterMsg<T>(Action<T> handler, NetChannelType type = NetChannelType.Main) where T : IMessage
        {
            if (_tcpInstances.TryGetValue(type, out var instance))
            {
                instance.RegisterMsg(handler);
            }
        }

        public void UnregisterMsg<T>(Action<T> handler, NetChannelType type = NetChannelType.Main) where T : IMessage
        {
            if (_tcpInstances.TryGetValue(type, out var instance))
            {
                instance.UnregisterMsg(handler);
            }
        }

        public void Shutdown()
        {
            foreach ((var k, var instance) in _tcpInstances)
            {
                instance.Dispose();
            }
            MsgTypeIdUtility.Clear();
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            foreach ((var k, var instance) in _tcpInstances)
            {
                instance.Update(elapseSeconds, realElapseSeconds);
            }
        }
    }
}
