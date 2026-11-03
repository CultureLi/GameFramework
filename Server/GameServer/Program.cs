using GameServer.Configuration;
using GameServer.Network;
using GameServer.Utility;

namespace GameServer;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
        }
        catch (IOException)
        {
            // 没有控制台时忽略
        }

        ServerConfig config;
        try
        {
            config = ServerConfig.Parse(args);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine(ex.Message);
            return 1;
        }

        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            Log.Info("收到退出信号，正在停止服务器...");
            cancellation.Cancel();
        };

        var server = new TcpGameServer(config);

        try
        {
            await server.StartAsync(cancellation.Token);
            return 0;
        }
        catch (InvalidOperationException ex)
        {
            // 配置类错误（例如端口被占用）只打印消息，不打印堆栈
            Log.Error(ex.Message);
            return 1;
        }
        catch (Exception ex)
        {
            Log.Error("服务器异常退出", ex);
            return 1;
        }
    }
}
