using System.Security.Cryptography;

namespace GameServer.Protocol;

/// <summary>
/// 与客户端 TcpInstance.Cryptor 对应的 AES 加解密：CBC + PKCS7。
/// 客户端固定复用同一个 IV，服务端回包也必须使用同一个 IV。
/// 正式项目建议改成每条消息随机 IV 并随包传输。
/// </summary>
public sealed class AesCrypto
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AesCrypto(byte[] key, byte[] iv)
    {
        if (key.Length is not (16 or 24 or 32))
            throw new ArgumentException($"AES key 长度非法: {key.Length}", nameof(key));
        if (iv.Length != 16)
            throw new ArgumentException($"AES IV 长度非法: {iv.Length}", nameof(iv));

        _key = (byte[])key.Clone();
        _iv = (byte[])iv.Clone();
    }

    public byte[] Encrypt(byte[] plain)
    {
        using var aes = CreateAes();
        using var encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(plain, 0, plain.Length);
    }

    public byte[] Decrypt(byte[] cipher)
    {
        if (cipher.Length == 0 || cipher.Length % 16 != 0)
            throw new InvalidDataException($"密文长度非法: {cipher.Length}");

        using var aes = CreateAes();
        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
    }

    private Aes CreateAes()
    {
        var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        return aes;
    }
}
