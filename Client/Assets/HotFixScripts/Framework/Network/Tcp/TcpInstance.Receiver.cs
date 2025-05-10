using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;
using UnityEngine.UIElements;

namespace Framework
{
    public partial class TcpInstance
    {
        private sealed class Receiver
        {
            private TcpInstance _instance;
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

                while (!_disposed)
                {
                    try
                    {
                        if (_connecter.IsConnected && stream.DataAvailable)
                        {
/*                            if (ReadMessageBlocking(stream, buffer, out var length, out var msgId, out var flag))
                            {

                                var packet = UnPack(buffer, length, msgId, flag);
                                _packets.Enqueue(packet);
                            }*/
                            var packet = UnPack(stream, buffer);
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

           /* public bool ReadMessageBlocking(NetworkStream stream, byte[] buffer, out int length, out uint msgId, out byte flag)
            {
                byte[] tempBuff = new byte[4];

                if (stream.ReadExactly(tempBuff, 4))
                    return false;
                int netVal = BitConverter.ToInt32(tempBuff, 0);
                length = IPAddress.NetworkToHostOrder(netVal);

                if (stream.ReadExactly(tempBuff, 4))
                    return false;

                netVal = BitConverter.ToInt32(tempBuff, 0);
                msgId = (uint)IPAddress.NetworkToHostOrder(netVal);

                if (stream.ReadExactly(tempBuff, 1))
                    return false;

                flag = tempBuff[0];

                if (length > 0 && length <= NetDefine.SCMaxMsgLen)
                {
                    return stream.ReadExactly(buffer, length);
                }
                Debug.LogWarning($"读取消息失败-type: {MsgTypeIdUtility.GetMsgType(msgId)} length: {length}");
                return false;
            }*/

            private SCPacket UnPack(NetworkStream stream, byte[] buffer)
            {
                byte[] headerBuff = new byte[NetDefine.SCHeaderLen];

                if (!stream.ReadExactly(headerBuff, NetDefine.SCHeaderLen))
                    return null;

                var offset = 0;
                var length = PackUtility.UnPackInt(headerBuff, ref offset);
                var msgId = (uint)PackUtility.UnPackInt(headerBuff, ref offset);
                var flag = PackUtility.UnPackByte(headerBuff, ref offset);

                if (length < 0 || length >= NetDefine.SCMaxMsgLen)
                    return null;


                if (!stream.ReadExactly(buffer, length))
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
