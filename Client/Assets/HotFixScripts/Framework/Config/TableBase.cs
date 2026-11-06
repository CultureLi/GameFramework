using System;
using System.IO;

namespace Framework
{
    /// <summary>
    /// 配置表基类。原先是 ConfigMgr.cs 里的内部类，抽到独立文件是为了让服务端
    /// 可以直接链接同一份源码（服务端不依赖 Unity / AOTBase）。
    /// </summary>
    public abstract class TableBase
    {
        public virtual bool UseOffset
        {
            get; private set;
        }

        public virtual void Initialize(
            string name,
            Func<string, MemoryStream> streamLoader,
            Func<string, int, int, ByteBuf> byteBufLoader)
        {
        }

        public virtual void Initialize(string name, Func<string, MemoryStream> streamLoader)
        {
        }
    }
}
