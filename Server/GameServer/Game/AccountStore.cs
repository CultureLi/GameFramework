using System.Security.Cryptography;
using System.Text;

namespace GameServer.Game;

public readonly record struct LoginResult(ulong PlayerId, int Code, string Message);

/// <summary>
/// 最简单的内存账号表：首次登录自动注册。
/// 仅用于基础流程，正式项目应换成数据库 + 密码哈希（bcrypt/scrypt/argon2）。
/// </summary>
public sealed class AccountStore
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, Account> _accounts = new(StringComparer.Ordinal);
    private long _nextPlayerId;

    public LoginResult Authenticate(string account, string password)
    {
        lock (_syncRoot)
        {
            if (_accounts.TryGetValue(account, out var existing))
            {
                if (!FixedTimeEquals(existing.Password, password))
                    return new LoginResult(0, LoginCodes.WrongPassword, "密码错误");

                return new LoginResult(existing.PlayerId, LoginCodes.Success, "登录成功");
            }

            var playerId = (ulong)++_nextPlayerId;
            _accounts[account] = new Account(password, playerId);
            return new LoginResult(playerId, LoginCodes.Success, "注册并登录成功");
        }
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length &&
               CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private sealed record Account(string Password, ulong PlayerId);
}
