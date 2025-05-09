using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace Framework
{
    public partial class TcpInstance
    {
        private sealed class Receiver
        {
            private Connecter _connecter;
            private Dispatcher _dispatcher;
            private bool _disposed;
            private Thread _thread;

            Queue<SCPacket> _packets = new Queue<SCPacket>();

            public Receiver(Connecter connecter, Dispatcher dispatcher)
            {
                _connecter = connecter;
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

                byte[] receiveBuffer = new byte[TcpDefine.SCMaxMsgLen];

                while (!_disposed)
                {
                    try
                    {
                        if (_connecter != null && _connecter.IsConnected && stream.DataAvailable)
                        {
                            if (!ReadMessageBlocking(stream, receiveBuffer, out var length, out var msgId))
                                break;

                            var packet = ReferencePool.Acquire<SCPacket>();
                            packet.msgId = msgId;

                            var type = TcpUtility.GetMsgType(msgId);
                            packet.msg = Activator.CreateInstance(type) as IMessage;

                            using (var codeStream = new CodedInputStream(receiveBuffer, 0, length))
                            {
                                packet.msg.MergeFrom(codeStream);
                            }

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
            public bool ReadMessageBlocking(NetworkStream stream, byte[] buffer, out int length, out uint msgId)
            {
                byte[] tempBuff = new byte[4];

                stream.ReadExactly(tempBuff, 4);
                int netVal = BitConverter.ToInt32(tempBuff, 0);
                length = IPAddress.NetworkToHostOrder(netVal);

                stream.ReadExactly(tempBuff, 4);
                netVal = BitConverter.ToInt32(tempBuff, 0);
                msgId = (uint)IPAddress.NetworkToHostOrder(netVal);

                if (length > 0 && length <= TcpDefine.SCMaxMsgLen)
                {
                    return stream.ReadExactly(buffer, length);
                }
                Debug.LogWarning("[Telepathy] ReadMessageBlocking: possible header attack with a header of: " + length + " bytes.");
                return false;
            }

            private readonly int _maxCntPerFrame = 100;
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
