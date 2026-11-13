using Google.Protobuf;

namespace Framework
{
    /// <summary>
    /// 接收到服务器的包
    /// </summary>
    public class SCPacket : Packet
    {
        public IMessage msg;

        public override void Clear()
        {
            base.Clear();
            // 不清空的话，池里的 SCPacket 会一直持有上一条 protobuf 消息，造成泄漏。
            msg = null;
        }
    }
}
