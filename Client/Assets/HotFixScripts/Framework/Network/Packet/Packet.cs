namespace Framework
{
    /// <summary>
    /// 网络消息包基类。
    /// </summary>
    public abstract class Packet : IReference
    {
        public uint id;

        public virtual void Clear()
        {
        }
    }
}
