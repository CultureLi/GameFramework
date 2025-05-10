using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Framework
{
    public partial class TcpInstance
    {
        private class Cryptor
        {
            TcpInstance _instance;
            Aes _aes;
            Action _exchangeKeyFinished;
            bool _isFinished;

            public Cryptor(TcpInstance instance, Action cb)
            {
                _instance = instance;
                _exchangeKeyFinished = cb;
            }

            public void Dispose()
            {
                _isFinished = false;
            }

            private void GenerateAESKey()
            {
                _aes = Aes.Create();
                _aes.GenerateKey();
                _aes.GenerateIV();

            }

            public void Update(float elapseSeconds, float realElapseSeconds)
            {
                if (_isFinished)
                    return;

                var stream = _instance.TCPClient.GetStream();

                if (_instance.IsConnected && stream.DataAvailable)
                {
                    ReceivePublicKey(stream);
                }
            }

            private void ReceivePublicKey(NetworkStream stream)
            {
                try
                {
                    byte[] headerBuff = new byte[4];
                    if (!stream.ReadExactly(headerBuff, 4))
                        throw new Exception("length read failed !!!");

                    var length = PackUtility.UnPackInt(headerBuff);

                    byte[] keyBytes = new byte[length];
                    if (!stream.ReadExactly(keyBytes, length))
                        throw new Exception("key read failed !!!");

                    var publicKey = Encoding.UTF8.GetString(keyBytes);
                    Console.WriteLine("收到 RSA 公钥");

                    SendEncryptedAESKey(stream, publicKey);

                    _isFinished = true;
                    _exchangeKeyFinished?.Invoke();
                }
                catch(Exception e)
                {
                    Debug.LogError($"ReceivePublicKey: {e}");
                }
            }


            private void SendEncryptedAESKey(NetworkStream stream, string publicKey)
            {

                GenerateAESKey();

                // 构造 key+iv 拼接字节
                byte[] combined = new byte[_aes.Key.Length + _aes.IV.Length];
                Buffer.BlockCopy(_aes.Key, 0, combined, 0, _aes.Key.Length);
                Buffer.BlockCopy(_aes.IV, 0, combined, _aes.Key.Length, _aes.IV.Length);

                // RSA 加密
                byte[] encryptedBytes;
                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(publicKey);
                    encryptedBytes = rsa.Encrypt(combined, RSAEncryptionPadding.Pkcs1); // 注意 padding 设置一致
                }

                // 构造完整发送 buffer
                int offset = 0;
                byte[] buffer = new byte[encryptedBytes.Length + 4];
                PackUtility.PackInt(encryptedBytes.Length, buffer, ref offset);  // 长度

                Buffer.BlockCopy(encryptedBytes, 0, buffer, offset, encryptedBytes.Length);

                // 发送
                stream.Write(buffer, 0, buffer.Length);
                Console.WriteLine("已发送加密的 AES 密钥");
            }

            public byte[] Encrypt(byte[] buff, int offset, int length)
            {
                using var encryptor = _aes.CreateEncryptor();
                return encryptor.TransformFinalBlock(buff, offset, length);
            }

            public byte[] Decrypt(byte[] buff, int offset, int length)
            {
                using var decryptor = _aes.CreateDecryptor();
                return decryptor.TransformFinalBlock(buff, offset, length);
            }
        }
    }
}
