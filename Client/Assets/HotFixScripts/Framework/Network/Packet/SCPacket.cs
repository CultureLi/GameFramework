using Framework;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework
{
    /// <summary>
    /// 接收到服务器的包
    /// </summary>
    public class SCPacket : Packet
    {
        public IMessage msg;
    }
}
