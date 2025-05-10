using System;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using System.Threading.Tasks;

namespace Framework
{
    public partial class TcpInstance
    {
        private sealed class Connecter
        {
            // Actions
            private Action<NetworkConnectState> _onConnectResult;

            public TcpClient TCPClient => _TcpClient;
            private TcpClient _TcpClient;

            public bool IsConnected =>
                _TcpClient != null &&
                _TcpClient.Client != null &&
                _TcpClient.Connected;

            public Connecter()
            {
                var family = Socket.OSSupportsIPv6 ?
                    AddressFamily.InterNetworkV6 :
                    AddressFamily.InterNetwork;

                _TcpClient = new TcpClient(family);
            }


            /// <summary>
            /// 连接-异步
            /// </summary>
            /// <param name="host"></param>
            /// <param name="port"></param>
            public async Task ConnectAsync(string host, int port, Action<NetworkConnectState> cb)
            {
                _onConnectResult = cb;
                if (_TcpClient == null || _TcpClient.Client == null)
                    return;

                if (_TcpClient.Connected)
                {
                    BroadcastConnectResult(NetworkConnectState.Connected);
                    return;
                }

                try
                {
                    Task connectTask;
                    if (IPAddress.TryParse(host, out var ip))
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetwork && Socket.OSSupportsIPv6)
                        {
                            ip = ip.MapToIPv6();
                        }
                        connectTask = _TcpClient.ConnectAsync(ip, port);
                    }
                    else
                    {
                        connectTask = _TcpClient.ConnectAsync(host, port);
                    }
                    // 判断两个Task哪个先完成
                    if (await Task.WhenAny(connectTask, Task.Delay(20000)) == connectTask)
                    {
                        if (connectTask.Status == TaskStatus.Faulted)
                        {
                            throw new Exception(connectTask.Exception.ToString());
                        }
                        else
                        {
                            BroadcastConnectResult(NetworkConnectState.Succeed);
                            Debug.Log($"连接结果 {IsConnected}");
                        }
                    }
                    else
                    {
                        BroadcastConnectResult(NetworkConnectState.Failed);
                        Debug.Log("连接超时！");
                    }
                }
                catch (Exception e)
                {
                    BroadcastConnectResult(NetworkConnectState.Failed);
                    Debug.LogError($"网络连接错误 ConnectAsync : {e}");
                }
            }

            /// <summary>
            /// 断开连接
            /// </summary>
            public void Disconnect()
            {
                if (!IsConnected)
                    return;

                try
                {
                    _TcpClient.Close();
                    _TcpClient = null;
                }
                catch (Exception e)
                {
                    Debug.LogError($"网络错误 Disconnect : {e}");
                }

                BroadcastConnectResult(NetworkConnectState.Disconnect);
            }

            /// <summary>
            /// 销毁
            /// </summary>
            public void Dispose()
            {
                Disconnect();
            }

            public void BroadcastConnectResult(NetworkConnectState result)
            {
                _onConnectResult?.Invoke(result);
            }
        }
    }
}
