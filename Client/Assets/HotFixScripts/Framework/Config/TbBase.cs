using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework
{
    public abstract class TbBase<T>
    {
        public abstract void Init(ByteBuf buf);
        public abstract List<T> DataList { get;}

    }
}
