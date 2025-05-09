using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Test.TestScripts.Runtime.NetTest
{
    internal class RsaKeyMgr
    {
        public string PublicKey
        {
            get; private set;
        }
        public string PrivateKey
        {
            get; private set;
        }

        private RSA rsa;

        public RsaKeyMgr()
        {
            rsa = RSA.Create();
            rsa.KeySize = 2048;  // 2048位常规用，4096更安全但更慢

            // 导出公钥和私钥（XML格式 或 PEM格式）
            PublicKey = rsa.ToXmlString(false);  // false 表示只导出公钥
            PrivateKey = rsa.ToXmlString(true);  // true 表示导出私钥+公钥
        }

        // 用公钥加密
        public byte[] Encrypt(byte[] data)
        {
            return rsa.Encrypt(data, RSAEncryptionPadding.Pkcs1);
        }

        // 用私钥解密
        public byte[] Decrypt(byte[] data)
        {
            return rsa.Decrypt(data, RSAEncryptionPadding.Pkcs1);
        }

        // 从XML字符串载入密钥
        public void LoadPublicKey(string publicKey)
        {
            rsa.FromXmlString(publicKey);
        }

        public void LoadPrivateKey(string privateKey)
        {
            rsa.FromXmlString(privateKey);
        }
    }
}
