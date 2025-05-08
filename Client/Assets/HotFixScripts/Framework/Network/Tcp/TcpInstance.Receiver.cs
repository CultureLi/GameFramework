using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Framework
{
    public partial class TcpInstance
    {
        private sealed class Receiver
        {
            private Connecter _connecter;
            private bool _disposed;
            private Thread _thread;
            public Receiver()
            {
                _thread = new Thread(() => ReceiveLoop())
                {
                    IsBackground = true
                };
                _thread.Start();
            }

            private void ReceiveLoop()
            {
                var stream = _connecter.TCPClient.GetStream();

                var maxMsgSize = 200000;
                byte[] receiveBuffer = new byte[maxMsgSize + 4];
                byte[] headerBuffer = new byte[4];

                while (true)
                {
                    if (_disposed)
                        break;

                    try
                    {
                        if (_connecter == null && _connecter.IsConnected)
                        {
                            if (!ReadMessageBlocking(stream, maxMsgSize, headerBuffer, receiveBuffer, out int size))
                                break;

                            ArraySegment<byte> message = new ArraySegment<byte>(receiveBuffer, 0, size);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"网络错误 ReceiveLoop {e}");
                    }

                }
            }
            public static bool ReadMessageBlocking(NetworkStream stream, int MaxMessageSize, byte[] headerBuffer, byte[] payloadBuffer, out int size)
            {
                size = 0;

                // buffer needs to be of Header + MaxMessageSize
                if (payloadBuffer.Length != 4 + MaxMessageSize)
                {
                    Debug.LogError($"[Telepathy] ReadMessageBlocking: payloadBuffer needs to be of size 4 + MaxMessageSize = {4 + MaxMessageSize} instead of {payloadBuffer.Length}");
                    return false;
                }

                // read exactly 4 bytes for header (blocking)
                if (!stream.ReadExactly(headerBuffer, 4))
                    return false;

                // convert to int
                size = BytesToIntBigEndian(headerBuffer);

                // protect against allocation attacks. an attacker might send
                // multiple fake '2GB header' packets in a row, causing the server
                // to allocate multiple 2GB byte arrays and run out of memory.
                //
                // also protect against size <= 0 which would cause issues
                if (size > 0 && size <= MaxMessageSize)
                {
                    // read exactly 'size' bytes for content (blocking)
                    return stream.ReadExactly(payloadBuffer, size);
                }
                Debug.LogWarning("[Telepathy] ReadMessageBlocking: possible header attack with a header of: " + size + " bytes.");
                return false;
            }

            public static int BytesToIntBigEndian(byte[] bytes)
            {
                return (bytes[0] << 24) |
                       (bytes[1] << 16) |
                       (bytes[2] << 8) |
                        bytes[3];
            }
        }
    }
}
