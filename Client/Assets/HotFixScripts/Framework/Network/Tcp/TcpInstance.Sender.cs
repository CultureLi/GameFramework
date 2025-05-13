using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace Framework
{
    public partial class TcpInstance
    {
        /// <summary>
        /// 消息发送器
        /// </summary>
        private sealed class Sender
        {
            private Thread _thread;
            Connecter _connecter;
            Cryptor _cryptor;
            private bool _disposed;

            private Queue<CSPacket> _packets = new Queue<CSPacket>();

            public Sender(Connecter connecter, Cryptor cryptor)
            {
                _connecter = connecter;
                _cryptor = cryptor;
                _thread = new Thread(() => SendLoop())
                {
                    IsBackground = true
                };
                _thread.Start();
            }

            public void Dispose()
            {
                _disposed = true;
            }

            private void SendLoop()
            {
                while (!_disposed)
                {
                    try
                    {
                        if (_connecter.IsConnected && _packets.Count > 0)
                        {
                            var packet = _packets.Dequeue();
                            if (packet != null)
                            {
                                NetworkStream networkStream = _connecter.TCPClient.GetStream();
                                networkStream.Write(packet.buff, 0, packet.length + NetDefine.CSHeaderLen);
                                
                                ReferencePool.Release(packet);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (e.GetType() != typeof(ObjectDisposedException))
                        {
                            Debug.LogError("Network sender run error:" + e.Message);
                        }
                        _connecter.TCPClient.Close();
                    }

                    Thread.Sleep(1);
                }
            }

            public void SendMsg(IMessage msg)
            {
                if (null == msg)
                    return;
                
                var packet = Pack(msg);
                _packets.Enqueue(packet);
            }

            /// <summary>
            /// 压包
            /// </summary>
            /// <param name="msg"></param>
            /// <returns></returns>
            byte[] _bodyBuffer = new byte[NetDefine.CSMaxMsgLen];
            byte[] _zipBuffer = new byte[NetDefine.CSMaxMsgLen];
            private readonly int _compressThreshold = 0;
            private CSPacket Pack(IMessage msg)
            {
                try
                {
                    var packet = ReferencePool.Acquire<CSPacket>();
                    packet.id = ProtoTypeHelper.GetMsgId(msg.GetType());
                    packet.flag = 0;
                    var length = msg.CalculateSize();

                    // 先序列化 msg 成为原始字节数组
                    using (var memStream = new MemoryStream(_bodyBuffer))
                    {
                        using (var codedStream = new CodedOutputStream(memStream))
                        {
                            msg.WriteTo(codedStream);
                            codedStream.Flush();
                        }
                    }

                    // 加密
                    packet.flag |= NetDefine.FlagCrypt;
                    //加密后字节数会变化, 因为会填充补齐数据
                    var buffer = _cryptor.Encrypt(_bodyBuffer, 0, length);
                    length = buffer.Length;
                    var originLength = length;

                    // 压缩
                    if ( originLength > _compressThreshold)
                    {
                        packet.flag |= NetDefine.FlagZip;
                        length = ZipHelper.Zip(buffer, buffer.Length, _zipBuffer);
                        buffer = _zipBuffer;
                    }

                    packet.length = length;
                    if (packet.length < 0 || packet.length > NetDefine.CSMaxMsgLen)
                    {
                        throw new Exception($"PackMsg - Msg Size Exception, type: {msg.GetType()} size: {packet.length}");
                    }

                    // 填写包头
                    var offset = 0;
                    PackHelper.PackInt(packet.length, packet.buff, ref offset);
                    PackHelper.PackInt((int)packet.id, packet.buff, ref offset);
                    PackHelper.PackByte(packet.flag, packet.buff, ref offset);

                    // 写入
                    Buffer.BlockCopy(buffer, 0, packet.buff, offset, packet.length);

                    return packet;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return null;
                }
            }

        }
    }
}
