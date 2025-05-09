using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 网络管理器。
    /// </summary>
    internal sealed partial class NetworkManager : IFramework, INetworkMgr
    {
        Dictionary<Type, uint> _typeToIdMap = new Dictionary<Type, uint>();

        /// <summary>
        /// 初始化网络管理器的新实例。
        /// </summary>
        public NetworkManager()
        {
            MsgTypeIdUtility.Init();
        }





        public void Shutdown()
        {
            MsgTypeIdUtility.Clear();
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {

        }
    }
}
