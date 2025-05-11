using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework
{
    public static class NetDefine
    {
        //消息头所占字节数
        //[length 4字节] 压缩、加密后消息Body长度
        //[originLength 4字节] 压缩前消息Body长度
        //[msgId 4字节]
        //[flag 1字节（表示加密、压缩等）]
        public static readonly int CSHeaderLen = 13; 
        public static readonly int SCHeaderLen = 13;
        public static readonly int CSMaxMsgLen = 1024*4;
        public static readonly int SCMaxMsgLen = 1024*10;

        //加密
        public static readonly byte FlagCrypt = 1;
        //压缩
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
}
