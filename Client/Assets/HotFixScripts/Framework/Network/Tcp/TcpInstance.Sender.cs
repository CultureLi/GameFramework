using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Framework
{
    public partial class TcpInstance
    {
        private sealed class Sender
        {
            private Thread _thread;
            private TcpClient _tcpClient;
            private bool _disposed;

            private class CSPacket : Packet
            {
                public int msgId;
                public int size;
                public byte[] buff = new byte[TcpDefine.CSMaxMsgLen];

                public int ToBytes(IMessage msg)
                {
                    int len = 0;
                    using (var ms = new MemoryStream(buff, TcpDefine.CSHeaderLen, TcpDefine.CSMaxMsgLen - TcpDefine.CSHeaderLen))
                    {
                        using (var cos = new CodedOutputStream(ms))
                        {
                            msg.Encode(cos);
                            cos.Flush();
                            len = (int)cos.Position;
                        }
                    }
                    return len;
                }
            }

            private Queue<CSPacket> _cSPackets = new Queue<CSPacket>();

            public Sender(TcpClient client)
            {
                _tcpClient = client;
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

            public void SetCryptKey(byte[] key)
            {
                //_cryptor = new AESCryptor(key);
            }

            private void SendLoop()
            {
                while (!_disposed)
                {
                    try
                    {
                        if (_tcpClient.Connected)
                        {
                            var packet = ReferencePool.Acquire<CSPacket>();
                            if (packet != null)
                            {
                                try
                                {
                                    NetworkStream networkStream = _tcpClient.GetStream();
                                    networkStream.Write(packet.Buff, 0, packet.Length);
                                }
                                catch (Exception msg)
                                {
                                    Debug.LogError($"Error send msg to server, msgID: {packet.MsgID} MsgName {ProtoBuf.PType.MsgID2Name(packet.MsgID)} . Error: {msg.Message}");
                                    _tcpClient.Close();
                                }

                                _packetPool.GivebackFreePacket(packet);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (e.GetType() != typeof(ObjectDisposedException))
                        {
                            Debug.LogError("Network sender run error:" + e.Message);
                        }
                        _tcpClient.Close();
                    }
                    Thread.Sleep(1);
                }
            }


            /// <summary>
            /// 将消息打包并放入发送队列
            /// </summary>
            public void SendMsg(IMessage msg, bool crypted)
            {
                //必须有要发送的消息
                if (null == msg)
                {
                    return;
                }
                if (crypted && _cryptor == null)
                {
                    crypted = false;
                    Debug.LogError("crypt key not set, can not send crypted msg.");
                }
                CSPacket packet = _packetPool.TakeFreePacket();
                packet.Init(msg, crypted);
                _packetPool.PushSendPacket(packet);
            }

            public void SendMsg(uint msgID, byte[] buffer, int buffLen, bool crypted)
            {
                //必须有要发送的消息
                if (null == buffer)
                {
                    return;
                }
                if (crypted && _cryptor == null)
                {
                    crypted = false;
                    Debug.LogError("crypt key not set, can not send crypted msg.");
                }
                CSPacket packet = _packetPool.TakeFreePacket();
                packet.Init(msgID, buffer, buffLen, crypted);
                _packetPool.PushSendPacket(packet);
            }
        }
    }
}
