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
