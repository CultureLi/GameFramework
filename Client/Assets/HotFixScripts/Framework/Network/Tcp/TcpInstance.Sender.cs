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
        private sealed class Sender
        {
            private Thread _thread;
            private Connecter _connecter;
            private bool _disposed;

            private Queue<CSPacket> _packets = new Queue<CSPacket>();

            public Sender(Connecter connecter)
            {
                _connecter = connecter;
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
                                    networkStream.Write(packet.buff, 0, packet.length + TcpDefine.CSHeaderLen);
                                }
                                catch (Exception e)
                                {
                                    Debug.LogError($"Send Error msgID: {TcpUtility.GetMsgType(packet.msgId).Name} {e}");
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
                
                var packet = ReferencePool.Acquire<CSPacket>();
                packet.Init(msg);

                _packets.Enqueue(packet);
            }
        }
    }
}
