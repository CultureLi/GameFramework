//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

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
}
