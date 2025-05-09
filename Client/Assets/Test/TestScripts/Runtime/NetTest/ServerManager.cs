using Framework;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Assets.TestScripts.Runtime.NetTest
{
    public class SCPacketEx : SCPacket
    {
        public uint connectId;
    }
    public class Connection
    {
        public TcpClient client;
        public NetworkStream stream;
        public Thread receiveThread;
        public Queue<SCPacketEx> receivePackets = new Queue<SCPacketEx>();
        public Queue<CSPacket> sendPackets = new Queue<CSPacket>();
        public bool isConnected;
        public uint connectionId;
    }

    public class ServerManager : MonoBehaviour
    {
        private TcpListener listener;
        private Thread listenThread;
        private bool isRunning = false;

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
                        if (!ReadMessageBlocking(conn.stream, receiveBuffer, out var length, out var msgId))
                            break;

                        var packet = ReferencePool.Acquire<SCPacketEx>();
                        packet.id = msgId;
                        packet.connectId = conn.connectionId;

                        var type = MsgTypeIdUtility.GetMsgType(msgId);
                        packet.msg = Activator.CreateInstance(type) as IMessage;
                        using (var codeStream = new CodedInputStream(receiveBuffer, 0, length))
                        {
                            packet.msg.MergeFrom(codeStream);
                        }

                        lock (conn.receivePackets)
                        {
                            conn.receivePackets.Enqueue(packet);
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

        public void SendMsg(uint connId, IMessage msg)
        {
            if (!connections.TryGetValue(connId, out var conn))
            {
                Debug.LogWarning($"连接 {connId} 不存在");
                return;
            }

            var packet = CSPacket.Create(msg);

            lock (conn.sendPackets)
            {
                conn.sendPackets.Enqueue(packet);
            }
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
