using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework
{
    public static class NetDefine
    {
        public static readonly int CSHeaderLen = 9; //字节[length 4字节] [msgId 4字节] [flag 1字节（表示加密、压缩等）]
        public static readonly int SCHeaderLen = 9;
        public static readonly int CSMaxMsgLen = 20000;
        public static readonly int SCMaxMsgLen = 200000;


        public static readonly byte FlagCrypt = 1;
        public static readonly byte FlagCompress = 1 << 1;
        
    }


    public enum NetworkConnectState
    {
        Failed = 0,     //失败
        Succeed = 1,    //成功
        Connected = 2,  //已连接
        Disconnect = 3, //断开连接
    }

    public enum NetChannelType
    {
        Main,
        Chat,
    }

    public enum NetFlag
    {
        
    }
}
