namespace GameServer.Game;

/// <summary>
/// 登录返回码，客户端 LoginAck.code 使用同一套值。
/// </summary>
public static class LoginCodes
{
    public const int Success = 0;
    public const int InvalidParam = 1;
    public const int WrongPassword = 2;
    public const int AlreadyOnline = 3;
    public const int ServerError = 4;
}
