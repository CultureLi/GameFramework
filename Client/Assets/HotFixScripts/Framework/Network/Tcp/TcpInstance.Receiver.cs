using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Framework
{
    public partial class TcpInstance
    {
        /// <summary>
        /// 消息接收器
        /// </summary>
        private sealed class Receiver
        {
            private bool _disposed;
            private Thread _thread;
            Connecter _connecter;
            Cryptor _cryptor;
            Dispatcher _dispatcher;

            Queue<SCPacket> _packets = new Queue<SCPacket>();

            public Receiver(Connecter connecter, Cryptor cryptor, Dispatcher dispatcher)
            {
                _connecter = connecter;
                _cryptor = cryptor;
                _dispatcher = dispatcher;

               

                _thread = new Thread(() => ReceiveLoop())
                {
                    IsBackground = true
                };
                _thread.Start();
            }

            public void Dispose()
            {
                _disposed = true;
            }

            private void ReceiveLoop()
            {
                var stream = _connecter.TCPClient.GetStream();
                while (!_disposed)
                {
                    try
                    {
                        if (_connecter.IsConnected && stream.DataAvailable)
                        {
                            var packet = UnPack(stream);
                            if(packet != null)
                                _packets.Enqueue(packet);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"网络错误 ReceiveLoop {e}");
                    }

                    Thread.Sleep(1);
                }
            }

            /// <summary>
            /// 解包
            /// </summary>
            /// <param name="stream"></param>
            /// <param name="buffer"></param>
            /// <returns></returns>
            byte[] bodyBuffer = new byte[NetDefine.SCMaxMsgLen];
            byte[] headerBuffer = new byte[NetDefine.SCHeaderLen];
            //解压buffer,zip压缩比一般在30%-70%，这个buffer设置大一些
            byte[] unZipBuffer = new byte[NetDefine.SCMaxMsgLen*10];
            private SCPacket UnPack(NetworkStream stream)
            {
                try
                {
                    if (!stream.ReadCompletely(headerBuffer, NetDefine.SCHeaderLen))
                        return null;

                    var offset = 0;
                    var length = PackHelper.UnPackInt(headerBuffer, ref offset);
                    var msgId = (uint)PackHelper.UnPackInt(headerBuffer, ref offset);
                    var flag = PackHelper.UnPackByte(headerBuffer, ref offset);
                    var type = ProtoTypeHelper.GetMsgType(msgId);

                    if (length < 0 || length >= NetDefine.SCMaxMsgLen)
                    {
                        throw new Exception($"PackMsg - type:{type} Size:{length}");
                    }

                    if (!stream.ReadCompletely(bodyBuffer, length))
                        return null;

                    var packet = ReferencePool.Acquire<SCPacket>();
                    packet.id = msgId;
                    packet.msg = Activator.CreateInstance(type) as IMessage;

                    var buffer = bodyBuffer;
                    var size = length;

                    //解压
                    if ((flag & NetDefine.FlagZip) != 0)
                    {
                        buffer = ZipHelper.UnZip(buffer, 0, length);
                        size = buffer.Length;
                    }

                    //解密
                    if ((flag & NetDefine.FlagCrypt) != 0)
                    {
                        buffer = _cryptor.Decrypt(buffer, 0, size);
                        size = buffer.Length;
                    }

                    packet.msg = packet.msg.Descriptor.Parser.ParseFrom(buffer, 0, size);

                    return packet;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return null;
                }
            }


            private readonly int _maxCntPerFrame = 50;
            /// <summary>
            /// 派发
            /// </summary>
            /// <param name="elapseSeconds"></param>
            /// <param name="realElapseSeconds"></param>
            public void Update(float elapseSeconds, float realElapseSeconds)
            {
                int msgCount = 0;
                while (_packets.Count > 0 && msgCount < _maxCntPerFrame)
                {
                    var packet = _packets.Dequeue();
                    _dispatcher.DispatchMsg(packet);
                    ReferencePool.Release(packet);
                }
            }
        }
    }
}
