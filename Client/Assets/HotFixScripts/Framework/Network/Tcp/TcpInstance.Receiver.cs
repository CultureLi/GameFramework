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
                // 等线程退出，避免它继续访问已经 Dispose 掉的连接/加密器。
                if (_thread != null && _thread.IsAlive && _thread != Thread.CurrentThread)
                {
                    _thread.Join(200);
                }
                _thread = null;
            }

            private void ReceiveLoop()
            {
                NetworkStream stream = null;
                try
                {
                    // 这行以前在 try 之外：重连/断开瞬间 TCPClient 为 null 时线程会直接抛异常退出，
                    // 没有日志也没有重连，表现为"突然收不到消息"。
                    var client = _connecter.TCPClient;
                    if (client == null)
                    {
                        Debug.LogWarning("[Network] Receiver 启动时连接已失效。");
                        return;
                    }

                    stream = client.GetStream();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return;
                }

                while (!_disposed)
                {
                    try
                    {
                        if (!_connecter.IsConnected)
                        {
                            Thread.Sleep(10);
                            continue;
                        }

                        if (stream.DataAvailable)
                        {
                            var packet = UnPack(stream);
                            if (packet != null)
                            {
                                _dispatcher.DispatchAsync(packet);
                            }
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (System.IO.IOException e)
                    {
                        // 连接被关闭：退出循环，不再刷日志。
                        Debug.LogWarning($"[Network] 连接已断开: {e.Message}");
                        break;
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
                SCPacket packet = null;
                try
                {
                    if (!stream.ReadCompletely(headerBuffer, NetDefine.SCHeaderLen))
                        return null;

                    var offset = 0;
                    var length = PackHelper.UnPackInt(headerBuffer, ref offset);
                    var msgId = (uint)PackHelper.UnPackInt(headerBuffer, ref offset);
                    var flag = PackHelper.UnPackByte(headerBuffer, ref offset);
                    var parser = ProtoTypeHelper.GetMsgParser(msgId);

                    // 缓冲刚好能放下 SCMaxMsgLen 字节，等于上限是合法的。
                    if (length < 0 || length > NetDefine.SCMaxMsgLen)
                    {
                        throw new Exception($"PackMsg - msgId:{msgId} Size:{length}");
                    }

                    if (!stream.ReadCompletely(bodyBuffer, length))
                        return null;

                    if (parser == null)
                    {
                        Debug.LogError($"[Network] 未注册的消息 id: {msgId}，丢弃该包。");
                        return null;
                    }

                    packet = ReferencePool.Acquire<SCPacket>();
                    packet.id = msgId;

                    var buffer = bodyBuffer;
                    var size = length;

                    //解压
                    if ((flag & NetDefine.FlagZip) != 0)
                    {
                        size = ZipHelper.UnZip(buffer, length, unZipBuffer);
                        if (size <= 0)
                        {
                            throw new Exception($"UnZip failed, msgId:{msgId}");
                        }
                        buffer = unZipBuffer;
                    }

                    //解密
                    if ((flag & NetDefine.FlagCrypt) != 0)
                    {
                        buffer = _cryptor.Decrypt(buffer, 0, size);
                        if (buffer == null)
                        {
                            throw new Exception($"Decrypt failed, msgId:{msgId}");
                        }
                        size = buffer.Length;
                    }

                    // 用缓存的 Parser 反序列化，避免每条消息多 new 一个空实例。
                    packet.msg = parser.ParseFrom(buffer, 0, size);

                    return packet;
                }
                catch (Exception e)
                {
                    if (packet != null)
                    {
                        ReferencePool.Release(packet);
                    }

                    Debug.LogException(e);
                    return null;
                }
            }
        }
    }
}
