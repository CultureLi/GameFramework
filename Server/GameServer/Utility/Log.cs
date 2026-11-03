namespace GameServer.Utility;

public static class Log
{
    private static readonly object SyncRoot = new();

    public static void Info(string message) => Write("INFO", message, ConsoleColor.Gray);

    public static void Warn(string message) => Write("WARN", message, ConsoleColor.Yellow);

    public static void Error(string message) => Write("ERROR", message, ConsoleColor.Red);

    public static void Error(string message, Exception exception) =>
        Write("ERROR", $"{message}: {exception}", ConsoleColor.Red);

    private static void Write(string level, string message, ConsoleColor color)
    {
        lock (SyncRoot)
        {
            var previous = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}");
            Console.ForegroundColor = previous;
        }
    }
}
