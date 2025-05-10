using Assets.Test.TestScripts.Runtime.NetTest;
using Framework;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEditor.Sprites;
using UnityEngine;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;

namespace Assets.Test.TestScripts.Runtime.NetTest
{
    public class AESCrypto
    {
        private Aes _aes;

        public AESCrypto(byte[] key, byte[] iv)
        {
            _aes = Aes.Create();
            _aes.Key = key;
            _aes.IV = iv;
        }

        public byte[] Encrypt(byte[] plain)
        {
            using var encryptor = _aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(plain, 0, plain.Length);
        }

        public byte[] Decrypt(byte[] cipher)
        {
            using var decryptor = _aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        }
    }

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

        RsaKeyMgr _rsaKeyMgr = new RsaKeyMgr();
        


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
                                byte[] decrypted = _rsaKeyMgr.Decrypt(buff);
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
                                var packet = UnPack(receiveBuffer, conn);

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
            Debug.LogWarning("[Telepathy] ReadMessageBlocking: possible header attack with a header of: " + length + " bytes.");
            return false;
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

        public void SendMsg(uint connId, IMessage msg, byte flag = 0)
        {
            if (!connections.TryGetValue(connId, out var conn))
            {
                Debug.LogWarning($"连接 {connId} 不存在");
                return;
            }

            var packet = PackMsg(msg, conn._aesCrypto);

            lock (conn.sendPackets)
            {
                conn.sendPackets.Enqueue(packet);
            }
        }
        private ServerPacket PackMsg(IMessage msg, AESCrypto crypto)
        {
            var packet = ReferencePool.Acquire<ServerPacket>();
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
            byte[] encryptedBytes = crypto.Encrypt(plainBytes);
            packet.length = encryptedBytes.Length;

            // 填写包头
            var offset = 0;
            PackUtility.PackInt(packet.length, packet.buff, ref offset); // 消息体长度（已加密）
            PackUtility.PackInt((int)packet.id, packet.buff, ref offset); // 消息 ID
            PackUtility.PackByte(packet.flag, packet.buff, ref offset);  // 标志位

            // 写入
            Buffer.BlockCopy(encryptedBytes, 0, packet.buff, offset, encryptedBytes.Length);

            return packet;
        }

        private ClientPacket UnPack(byte[] buffer, Connection conn)
        {
            var stream = conn.stream;
            byte[] tempBuff = new byte[4];

            stream.ReadExactly(tempBuff, 4);
            int netVal = BitConverter.ToInt32(tempBuff, 0);
            var length = IPAddress.NetworkToHostOrder(netVal);

            stream.ReadExactly(tempBuff, 4);
            netVal = BitConverter.ToInt32(tempBuff, 0);
            var msgId = (uint)IPAddress.NetworkToHostOrder(netVal);

            stream.ReadExactly(tempBuff, 1);
            var flag = tempBuff[0];

            buffer = new byte[length];
            if (length > 0 && length <= NetDefine.SCMaxMsgLen)
            {
                stream.ReadExactly(buffer, length);
            }

            var packet = ReferencePool.Acquire<ClientPacket>();
            packet.id = msgId;

            packet.connectId = conn.connectionId;

            var type = MsgTypeIdUtility.GetMsgType(msgId);
            packet.msg = Activator.CreateInstance(type) as IMessage;


            if ((flag & NetDefine.FlagCrypt) != 0)
            {
                buffer = conn._aesCrypto.Decrypt(buffer);
            }
            length = buffer.Length;
            if ((flag & NetDefine.FlagCompress) != 0)
            {

            }


            using (var codeStream = new CodedInputStream(buffer, 0, length))
            {
                packet.msg.MergeFrom(codeStream);
            }
            return packet;
        }

        public void SendPublicKey(Connection conn)
        {
            var buff = new byte[NetDefine.SCMaxMsgLen];

            byte[] publicKeyBytes = Encoding.UTF8.GetBytes(_rsaKeyMgr.PublicKey);
            
            var length = publicKeyBytes.Length;

            var offset = 0;
            PackUtility.PackInt(length, buff, ref offset);

            // 3. 将公钥数据写入 buff（紧跟包头之后）
            Buffer.BlockCopy(publicKeyBytes, 0, buff, offset, length);

            conn.stream.Write(buff, 0, length + 4);
            Debug.Log($"服务端发送 public Key{_rsaKeyMgr.PublicKey}");

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
                        var type = MsgTypeIdUtility.GetMsgType(packet.id);
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
