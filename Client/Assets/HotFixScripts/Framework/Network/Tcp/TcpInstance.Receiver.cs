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

                byte[] buffer = new byte[NetDefine.SCMaxMsgLen];
                byte[] headerBuffer = new byte[NetDefine.SCHeaderLen];

                while (!_disposed)
                {
                    try
                    {
                        if (_connecter.IsConnected && stream.DataAvailable)
                        {
                            var packet = UnPack(stream, headerBuffer, buffer);
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
            private SCPacket UnPack(NetworkStream stream, byte[] headerBuffer, byte[] buffer)
            {
                try
                {
                    if (!stream.ReadCompletely(headerBuffer, NetDefine.SCHeaderLen))
                        return null;

                    var offset = 0;
                    var length = PackUtility.UnPackInt(headerBuffer, ref offset);
                    var msgId = (uint)PackUtility.UnPackInt(headerBuffer, ref offset);
                    var flag = PackUtility.UnPackByte(headerBuffer, ref offset);

                    if (length < 0 || length >= NetDefine.SCMaxMsgLen)
                        return null;

                    if (!stream.ReadCompletely(buffer, length))
                        return null;

                    var packet = ReferencePool.Acquire<SCPacket>();
                    packet.id = msgId;

                    var type = MsgTypeIdUtility.GetMsgType(msgId);
                    packet.msg = Activator.CreateInstance(type) as IMessage;

                    var decryptBytes = buffer;
                    if ((flag & NetDefine.FlagCrypt) != 0)
                    {
                        decryptBytes = _cryptor.Decrypt(buffer, 0, length);
                        length = decryptBytes.Length;
                    }

                    if ((flag & NetDefine.FlagCompress) != 0)
                    {

                    }


                    using (var codeStream = new CodedInputStream(decryptBytes, 0, length))
                    {
                        packet.msg.MergeFrom(codeStream);
                    }
                    return packet;
                }
                catch (Exception e)
                {
                    Debug.LogError($"UnPack Error: {e}");
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
