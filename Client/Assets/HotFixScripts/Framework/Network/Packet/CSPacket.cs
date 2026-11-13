namespace Framework
{
    /// <summary>
    /// 发向服务器的包
    /// </summary>
    public class CSPacket : Packet
    {
        public int length;
        public byte flag;
        // 包头和包体放在同一块缓冲里，容量必须包含包头，
        // 否则 length == CSMaxMsgLen 时会写越界（原来固定 4096，头部 9 字节写不进去）。
        public byte[] buff = new byte[NetDefine.CSHeaderLen + NetDefine.CSMaxMsgLen];

        public override void Clear()
        {
            base.Clear();
            length = 0;
            flag = 0;
        }
    }
}
