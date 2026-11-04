namespace GameServer.Game;

/// <summary>
/// 寻路返回码，客户端 PathFindAck.code 使用同一套值。
/// </summary>
public static class PathFindCodes
{
    public const int Success = 0;
    public const int NotLoggedIn = 1;
    public const int InvalidTarget = 2;
    public const int Unreachable = 3;
    public const int ServerError = 4;
}
