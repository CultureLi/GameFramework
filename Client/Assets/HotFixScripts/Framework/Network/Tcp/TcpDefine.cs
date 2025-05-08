using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework
{
    internal static class TcpDefine
    {
        public static readonly int CSHeaderLen = 8;
        public static readonly int SCHeaderLen = 8;
        public static readonly int CSMaxMsgLen = 20000;
    }
}
