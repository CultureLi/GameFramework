using Google.Protobuf;

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
