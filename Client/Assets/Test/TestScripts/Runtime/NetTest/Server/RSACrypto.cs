using System;
using System.Security.Cryptography;
using UnityEngine;

namespace TestRuntime
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

        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="buff"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public byte[] Encrypt(byte[] buff, int offset, int length)
        {
            try
            {
                using var encryptor = _aes.CreateEncryptor();
                return encryptor.TransformFinalBlock(buff, offset, length);
            }
            catch (Exception e)
            {
                Debug.LogError($"Encrypt Error: {e}");
                return null;
            }
        }

        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="buff"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public byte[] Decrypt(byte[] buff, int offset, int length)
        {
            try
            {
                using var decryptor = _aes.CreateDecryptor();
                return decryptor.TransformFinalBlock(buff, offset, length);
            }
            catch (Exception e)
            {
                Debug.LogError($"Decrypt Error: {e}");
                return null;
            }
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
