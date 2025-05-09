using Google.Protobuf;
using System;
using System.IO;
using System.Net;

namespace Framework
{
    /// <summary>
    /// 网络消息包基类。
    /// </summary>
    public abstract class Packet : IReference
    {
        public virtual int Id { get; set; }

        public virtual void Clear()
        {
        }
    }

    /// <summary>
    /// 客户端发向服务器的包
    /// </summary>
    public class CSPacket : Packet
    {
        public uint msgId;
        public int length;
        public byte[] buff = new byte[TcpDefine.CSMaxMsgLen];

        public void Init(IMessage msg)
        {
            msgId = TcpUtility.GetMsgId(msg.GetType());
            length = msg.CalculateSize();

            using (var memStream = new MemoryStream(buff, TcpDefine.CSHeaderLen, TcpDefine.CSMaxMsgLen-TcpDefine.CSHeaderLen))
            {
                using (var codedStream = new CodedOutputStream(memStream))
                {
                    msg.WriteTo(codedStream);
                    codedStream.Flush();
                }
            }

            PackInt(length, buff, 0);
            PackInt((int)msgId, buff, 4);
        }

        public static void PackInt(int val, byte[] buf, int offset = 0)
        {
            int netVal = IPAddress.HostToNetworkOrder(val);
            byte[] intBuf = BitConverter.GetBytes(netVal);
            for (int i = 0; i < intBuf.GetLength(0); i++)
            {
                buf[offset + i] = intBuf[i];
            }
        }
    }

    public class SCPacket : Packet
    {
        public uint msgId;
        public IMessage msg;
    }
}
