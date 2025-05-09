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
    /// 客户端发向服务器的包
    /// </summary>
    public class CSPacket : Packet
    {
        public int length;
        public byte[] buff = new byte[NetDefine.CSMaxMsgLen];

        public static CSPacket Create(IMessage msg)
        {
            var packet = ReferencePool.Acquire<CSPacket>();
            packet.id = MsgTypeIdUtility.GetMsgId(msg.GetType());
            packet.length = msg.CalculateSize();

            using (var memStream = new MemoryStream(packet.buff, NetDefine.CSHeaderLen, NetDefine.CSMaxMsgLen - NetDefine.CSHeaderLen))
            {
                using (var codedStream = new CodedOutputStream(memStream))
                {
                    msg.WriteTo(codedStream);
                    codedStream.Flush();
                }
            }

            PackUtility.PackInt(packet.length, packet.buff, 0);
            PackUtility.PackInt((int)packet.id, packet.buff, 4);
            return packet;
        }
    }
}
