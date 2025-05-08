using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class TcpServer
{
    static void Main(string[] args)
    {
        // 创建监听 socket
        TcpListener listener = new TcpListener(IPAddress.Any, 8888);
        listener.Start();
        Console.WriteLine("服务器已启动，监听端口 8888...");

        while (true)
        {
            // 接受客户端连接
            TcpClient client = listener.AcceptTcpClient();
            Console.WriteLine("有客户端连接进来了！");

            // 每个客户端分配一个线程处理
            Thread clientThread = new Thread(HandleClient);
            clientThread.Start(client);
        }
    }

    static void HandleClient(object obj)
    {
        TcpClient client = (TcpClient)obj;
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        try
        {
            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    Console.WriteLine("客户端断开连接。");
                    break;
                }

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"收到客户端消息: {message}");

                // 回复客户端
                string response = "Hello Client";
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                stream.Write(responseBytes, 0, responseBytes.Length);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("客户端异常断开: " + ex.Message);
        }
        finally
        {
            stream.Close();
            client.Close();
        }
    }
}
