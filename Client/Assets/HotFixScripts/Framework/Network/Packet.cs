using Google.Protobuf;
using System.IO;

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
        public uint length;
        public byte[] buff = new byte[TcpDefine.CSMaxMsgLen];

        public void Init(IMessage msg)
        {
            msgId = TcpUtility.GetMsgId(msg.GetType());
            length = (uint)msg.CalculateSize();

            using (var memStream = new MemoryStream(buff, TcpDefine.CSHeaderLen, TcpDefine.CSMaxMsgLen - TcpDefine.CSHeaderLen))
            {
                using (var codedStream = new CodedOutputStream(memStream))
                {
                    codedStream.WriteUInt32(length);
                    codedStream.WriteUInt32(msgId);
                    msg.WriteTo(codedStream);
                    
                    codedStream.Flush();
                }
            } 
        }
    }
}
