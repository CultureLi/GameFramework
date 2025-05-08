using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Framework
{
    /// <summary>
    /// 网络管理器。
    /// </summary>
    internal sealed partial class NetworkManager : IFramework, INetworkMgr
    {
    
        /// <summary>
        /// 初始化网络管理器的新实例。
        /// </summary>
        public NetworkManager()
        {
           
        }

        public void Shutdown()
        {
            throw new NotImplementedException();
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            throw new NotImplementedException();
        }
    }
}
