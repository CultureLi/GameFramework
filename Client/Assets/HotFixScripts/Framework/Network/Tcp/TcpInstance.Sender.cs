using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using UnityEditor.Sprites;
using UnityEngine;

namespace Framework
{
    public partial class TcpInstance
    {
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

            public bool Disposed
            {
                get
                {
                    return _disposed;
                }
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
                                try
                                {
                                    NetworkStream networkStream = _connecter.TCPClient.GetStream();
                                    networkStream.Write(packet.buff, 0, packet.length + NetDefine.CSHeaderLen);
                                }
                                catch (Exception e)
                                {
                                    Debug.LogError($"Send Error msgID: {MsgTypeIdUtility.GetMsgType(packet.id).Name} {e}");
                                    _connecter.TCPClient.Close();
                                }

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
                
                var packet = PackMsg(msg);
                _packets.Enqueue(packet);
            }

            private CSPacket PackMsg(IMessage msg)
            {
                var packet = ReferencePool.Acquire<CSPacket>();
                packet.id = MsgTypeIdUtility.GetMsgId(msg.GetType());
                packet.flag = 0;

                // 先序列化 msg 成为原始字节数组
                byte[] plainBytes;
                using (var memStream = new MemoryStream())
                {
                    using (var codedStream = new CodedOutputStream(memStream))
                    {
                        msg.WriteTo(codedStream);
                        codedStream.Flush();
                    }
                    plainBytes = memStream.ToArray();
                }


                // AES 加密
                packet.flag |= NetDefine.FlagCrypt;
                byte[] encryptedBytes = _cryptor.Encrypt(plainBytes, 0, plainBytes.Length);
                packet.length = encryptedBytes.Length;

                // 填写包头
                var offset = 0;
                PackUtility.PackInt(packet.length, packet.buff, ref offset); // 消息体长度（已加密）
                PackUtility.PackInt((int)packet.id, packet.buff, ref offset); // 消息 ID
                PackUtility.PackByte(packet.flag, packet.buff, ref offset);  // 标志位：标记已加密

                // 写入
                Buffer.BlockCopy(encryptedBytes, 0, packet.buff, offset, encryptedBytes.Length);

                return packet;
            }

        }
    }
}
