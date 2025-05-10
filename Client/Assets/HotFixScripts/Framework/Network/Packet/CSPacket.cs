using Framework;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework
{
    /// <summary>
    /// 发向服务器的包
    /// </summary>
    public class CSPacket : Packet
    {
        public int length;
        public byte flag;
        public byte[] buff = new byte[NetDefine.CSMaxMsgLen];

    }
}
