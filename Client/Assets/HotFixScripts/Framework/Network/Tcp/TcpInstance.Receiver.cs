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
            private TcpInstance _instance;
            private bool _disposed;
            private Thread _thread;

            Queue<SCPacket> _packets = new Queue<SCPacket>();

            public Receiver(TcpInstance instance)
            {
                _instance = instance;
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
                var stream = _instance.TCPClient.GetStream();

                byte[] buffer = new byte[NetDefine.SCMaxMsgLen];

                while (!_disposed)
                {
                    try
                    {
                        if (_instance.IsConnected && stream.DataAvailable)
                        {
                            if (!ReadMessageBlocking(stream, buffer, out var length, out var msgId))
                                break;

                            var packet = SCPacket.Create(msgId, buffer, length);

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

                if (length > 0 && length <= NetDefine.SCMaxMsgLen)
                {
                    return stream.ReadExactly(buffer, length);
                }
                Debug.LogWarning($"读取消息失败-type: {MsgTypeIdUtility.GetMsgType(msgId)} length: {length}");
                return false;
            }

            private readonly int _maxCntPerFrame = 100;
            public void Update(float elapseSeconds, float realElapseSeconds)
            {
                int msgCount = 0;
                while (_packets.Count > 0 && msgCount < _maxCntPerFrame)
                {
                    var packet = _packets.Dequeue();
                    _instance.DispatchMsg(packet);
                    ReferencePool.Release(packet);
                }
            }
        }
    }
}
