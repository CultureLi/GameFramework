using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Runtime.NetTest
{
    public class AESCrypto
    {
        private Aes _aes;

        public AESCrypto(byte[] key, byte[] iv)
        {
            _aes = Aes.Create();
            _aes.Key = key;
            _aes.IV = iv;
        }

        public byte[] Encrypt(byte[] plain)
        {
            using var encryptor = _aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(plain, 0, plain.Length);
        }

        public byte[] Decrypt(byte[] cipher)
        {
            using var decryptor = _aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        }
    }

    internal class RSACrypto
    {
        public string PublicKey
        {
            get; private set;
        }
        public string PrivateKey
        {
            get; private set;
        }

        RSA _rsa;

        public RSACrypto()
        {
            _rsa = RSA.Create();
            _rsa.KeySize = 2048;  // 2048位常规用，4096更安全但更慢

            // 导出公钥和私钥（XML格式 或 PEM格式）
            PublicKey = _rsa.ToXmlString(false);  // false 表示只导出公钥
            PrivateKey = _rsa.ToXmlString(true);  // true 表示导出私钥+公钥
        }

        // 用私钥解密
        public byte[] RSADecrypt(byte[] data)
        {
            return _rsa.Decrypt(data, RSAEncryptionPadding.Pkcs1);
        }
    }
}
