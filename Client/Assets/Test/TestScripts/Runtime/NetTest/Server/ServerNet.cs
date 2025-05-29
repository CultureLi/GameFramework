using Framework;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Test.Runtime.NetTest
{
    

    public class ClientPacket : SCPacket
    {
        public uint connectId;
    }

    public class ServerPacket : CSPacket
    {
    }


    public class Connection
    {
        public TcpClient client;
        public NetworkStream stream;
        public Thread receiveThread;
        public Queue<ClientPacket> receivePackets = new Queue<ClientPacket>();
        public Queue<ServerPacket> sendPackets = new Queue<ServerPacket>();
        public bool isConnected;
        public uint connectionId;
        public AESCrypto _aesCrypto;
    }

    public class ServerNet : MonoBehaviour
    {
        private TcpListener listener;
        private Thread listenThread;
        private bool isRunning = false;

        RSACrypto _cryptor = new RSACrypto();
       
        Dictionary<uint, Connection> connections = new Dictionary<uint, Connection>();

        Dictionary<System.Type, List<Delegate>> handlerMap = new Dictionary<System.Type, List<Delegate>>();

        void Start()
        {
            RegisterMsg<MonsterInfoReq>(OnMonsterInfoReq);
            StartServer(8888);
        }

        private void OnDestroy()
        {
            StopServer();
        }


        public void StartServer(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            isRunning = true;

            listenThread = new Thread(ListenLoop);
            listenThread.Start();
            Debug.Log("服务器已启动，监听端口: " + port);
        }

        public void StopServer()
        {
            isRunning = false;
            listener?.Stop();
            listenThread?.Abort();
            Debug.Log("服务器已关闭");
        }

        uint connectId = 1;
        public uint GenConnectId()
        {
            return connectId++;
        }

        private void ListenLoop()
        {
            while (isRunning)
            {
                try
                {
                    if (listener.Pending())
                    {
                        TcpClient client = listener.AcceptTcpClient();
                        Debug.Log("客户端已连接: " + client.Client.RemoteEndPoint);

                        var conn = new Connection
                        {
                            client = client,
                            stream = client.GetStream(),
                            isConnected = true,
                            connectionId = GenConnectId()
                        };

                        connections[conn.connectionId] = conn;

                        conn.receiveThread = new Thread(() => HandleClient(conn));
                        conn.receiveThread.Start();

                        //发送公钥
                        //SendMsg(conn.connectionId, new ServerPublicKey() { Key = _rsaKeyMgr.PublicKey });
                        SendPublicKey(conn);
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("监听异常: " + ex.Message);
                }
            }
        }

        private void HandleClient(Connection conn)
        {
            byte[] receiveBuffer = new byte[NetDefine.SCMaxMsgLen];
            try
            {
                while (conn.isConnected)
                {
                    if (conn.stream.DataAvailable)
                    {
                        try
                        {
                            if (conn._aesCrypto == null)
                            {
                                Debug.Log("收到Client AES key");
                                byte[] tempBuff = new byte[4];

                                conn.stream.ReadExactly(tempBuff, 4);
                                int netVal = BitConverter.ToInt32(tempBuff, 0);
                                var len = IPAddress.NetworkToHostOrder(netVal);

                                var buff = new byte[len];
                                conn.stream.ReadExactly(buff, len);
                                byte[] decrypted = _cryptor.RSADecrypt(buff);
                                // 解析出 AES Key + IV
                                int keySize = 32; // AES-256
                                int ivSize = 16;  // AES 默认 IV 大小

                                if (decrypted.Length < keySize + ivSize)
                                    throw new Exception("解密数据长度不足");

                                byte[] aesKey = new byte[keySize];
                                byte[] aesIV = new byte[ivSize];
                                Buffer.BlockCopy(decrypted, 0, aesKey, 0, keySize);
                                Buffer.BlockCopy(decrypted, keySize, aesIV, 0, ivSize);

                                conn._aesCrypto = new AESCrypto(aesKey, aesIV);
                            }
                            else
                            {
                                var packet = UnPack(conn.stream, conn);

                                lock (conn.receivePackets)
                                {
                                    conn.receivePackets.Enqueue(packet);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                        
                    }
                    else
                    {
                        Thread.Sleep(50);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"客户端通信异常: {ex.Message}");
            }
            finally
            {
                conn.client.Close();
                connections.Remove(conn.connectionId);
                Debug.Log($"客户端断开: {conn.client.Client.RemoteEndPoint}");
            }
        }

        public void RegisterMsg<T>(Action<uint,T> handler) where T : IMessage
        {
            var msgType = typeof(T);
            if (!handlerMap.TryGetValue(msgType, out var list))
            {
                list = new List<Delegate>();
                handlerMap[msgType] = list;
            }
            if (!list.Contains(handler))
                list.Add(handler);
        }

        private void OnMonsterInfoReq(uint connectId, MonsterInfoReq req)
        {
            Debug.Log($"Server 收到 MonsterInfoReq {req.Id}");
            var ack = new MonsterInfoAck();
            ack.Data = new MonsterData();

            ack.Data.Buffs.Add(new BuffData() { Id = 2 });
            ack.Data.Buffs.Add(new BuffData() { Id = 10 });
            ack.Data.Buffs.Add(new BuffData() { Id = 400 });

            SendMsg(connectId, ack);
        }

        public void SendMsg(uint connId, IMessage msg)
        {
            if (!connections.TryGetValue(connId, out var conn))
            {
                Debug.LogWarning($"连接 {connId} 不存在");
                return;
            }

            var packet = Pack(msg, conn);

            lock (conn.sendPackets)
            {
                conn.sendPackets.Enqueue(packet);
            }
        }

        int _compressThreshold = 0;
        byte[] zipBuffer = new byte[NetDefine.CSMaxMsgLen];
        private ServerPacket Pack(IMessage msg, Connection conn)
        {
            var _cryptor = conn._aesCrypto;
            try
            {
                var packet = ReferencePool.Acquire<ServerPacket>();
                packet.id = ProtoTypeHelper.GetMsgId(msg.GetType());
                packet.flag = 0;
                var length = msg.CalculateSize();

                // 先序列化 msg 成为原始字节数组
                using (var memStream = new MemoryStream(bodyBuffer))
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
                var buffer = _cryptor.Encrypt(bodyBuffer, 0, length);
                length = buffer.Length;
                var originLength = length;

                // 压缩
                if (originLength > _compressThreshold)
                {
                    packet.flag |= NetDefine.FlagZip;
                    length = ZipHelper.Zip(buffer, buffer.Length, zipBuffer);
                    buffer = zipBuffer;
                }

                packet.length = length;
                if (length < 0 || length > NetDefine.CSMaxMsgLen)
                {
                    throw new Exception($"PackMsg - Origin Msg Size Exception, type: {msg.GetType()} size: {length}");
                }

                // 填写包头
                var offset = 0;
                PackHelper.PackInt(packet.length, packet.buff, ref offset); // 消息体长度
                                                                            //PackHelper.PackInt(originLength, packet.buff, ref offset); // 消息体原始长度
                PackHelper.PackInt((int)packet.id, packet.buff, ref offset); // 消息 ID
                PackHelper.PackByte(packet.flag, packet.buff, ref offset);  // 标志位：标记已加密

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

        /// <summary>
        /// 解包
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        byte[] bodyBuffer = new byte[NetDefine.SCMaxMsgLen];
        byte[] headerBuffer = new byte[NetDefine.SCHeaderLen];
        byte[] unZipBuffer = new byte[NetDefine.SCMaxMsgLen*10];
        private ClientPacket UnPack(NetworkStream stream, Connection conn)
        {
            var _cryptor = conn._aesCrypto;
            try
            {
                if (!stream.ReadCompletely(headerBuffer, NetDefine.SCHeaderLen))
                    return null;

                var offset = 0;
                var length = PackHelper.UnPackInt(headerBuffer, ref offset);
                //var originLength = PackHelper.UnPackInt(headerBuffer, ref offset);
                var msgId = (uint)PackHelper.UnPackInt(headerBuffer, ref offset);
                var flag = PackHelper.UnPackByte(headerBuffer, ref offset);
                var type = ProtoTypeHelper.GetMsgType(msgId);

                if (length < 0 || length >= NetDefine.SCMaxMsgLen)
                {
                    throw new Exception($"PackMsg - type:{type} Size:{length}");
                }

                if (!stream.ReadCompletely(bodyBuffer, length))
                    return null;

                var packet = ReferencePool.Acquire<ClientPacket>();
                packet.id = msgId;
                packet.msg = Activator.CreateInstance(type) as IMessage;

                var buffer = bodyBuffer;
                var size = length;

                //解压
                if ((flag & NetDefine.FlagZip) != 0)
                {
                    size = ZipHelper.UnZip(buffer, length, unZipBuffer);
                    buffer = unZipBuffer;
                }

                //解密
                if ((flag & NetDefine.FlagCrypt) != 0)
                {
                    buffer = _cryptor.Decrypt(buffer, 0, size);
                    size = buffer.Length;
                }
                packet.msg = packet.msg.Descriptor.Parser.ParseFrom(buffer, 0, size);

                packet.connectId = conn.connectionId;
                return packet;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        public void SendPublicKey(Connection conn)
        {
            var buff = new byte[NetDefine.SCMaxMsgLen];

            byte[] publicKeyBytes = Encoding.UTF8.GetBytes(_cryptor.PublicKey);
            
            var length = publicKeyBytes.Length;

            var offset = 0;
            PackHelper.PackInt(length, buff, ref offset);

            // 3. 将公钥数据写入 buff（紧跟包头之后）
            Buffer.BlockCopy(publicKeyBytes, 0, buff, offset, length);

            conn.stream.Write(buff, 0, length + 4);
            Debug.Log($"服务端发送 public Key{_cryptor.PublicKey}");

        }

        public void Update()
        {
            foreach (var kv in connections)
            {
                var conn = kv.Value;

                // 处理接收到的消息
                lock (conn.receivePackets)
                {
                    while (conn.receivePackets.Count > 0)
                    {
                        var packet = conn.receivePackets.Dequeue();
                        var type = ProtoTypeHelper.GetMsgType(packet.id);
                        if (handlerMap.TryGetValue(type, out var handlerList))
                        {
                            foreach (var handler in handlerList)
                            {
                                handler.DynamicInvoke(packet.connectId, packet.msg);
                            }
                        }
                        ReferencePool.Release(packet);
                    }
                }

                // 发送消息
                lock (conn.sendPackets)
                {
                    while (conn.sendPackets.Count > 0)
                    {
                        var packet = conn.sendPackets.Dequeue();
                        try
                        {
                            conn.stream.Write(packet.buff, 0, packet.length + NetDefine.CSHeaderLen);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Send Error to Conn {conn.connectionId}: {e}");
                            conn.client.Close();
                        }
                        ReferencePool.Release(packet);
                    }
                }
            }
        }

    }

}
