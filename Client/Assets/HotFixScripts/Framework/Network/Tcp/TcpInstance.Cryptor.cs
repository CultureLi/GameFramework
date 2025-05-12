using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Framework
{
    public partial class TcpInstance
    {
        /// <summary>
        /// 加密器，使用AES对称加密，在连接到Server后，Server会将已生成的RSA非对称加密的公钥（直接明文）发送给client
        /// client收到后，创建AES Key,然后将 AES Key使用公钥加密，发送给Server, Server收到后使用私钥解密
        /// Server会保留每个链接的AES Key, 以后的通信双端都使用这套Key进行加解密
        /// </summary>
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
                Debug.Log("Generate AES Key");
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

            /// <summary>
            /// 接收server发来的公钥
            /// </summary>
            /// <param name="stream"></param>
            private void ReceivePublicKey(NetworkStream stream)
            {
                try
                {
                    byte[] headerBuff = new byte[4];
                    if (!stream.ReadCompletely(headerBuff, 4))
                        throw new Exception("ReceivePublicKey length read failed !!!");

                    var length = PackHelper.UnPackInt(headerBuff);

                    byte[] keyBytes = new byte[length];
                    if (!stream.ReadCompletely(keyBytes, length))
                        throw new Exception("ReceivePublicKey key read failed !!!");

                    var publicKey = Encoding.UTF8.GetString(keyBytes);

                    SendEncryptedAESKey(stream, publicKey);

                    _isFinished = true;
                    _exchangeKeyFinished?.Invoke();
                }
                catch(Exception e)
                {
                    Debug.LogException(e);
                }
            }

            /// <summary>
            /// 生成AES key,使用公钥加密发送给Server
            /// </summary>
            /// <param name="stream"></param>
            /// <param name="publicKey"></param>
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
                    // 用公钥加密，注意 padding 跟Server设置一致
                    encryptedBytes = rsa.Encrypt(combined, RSAEncryptionPadding.Pkcs1); 
                }

                // 构造完整发送 bodyBuffer
                int offset = 0;
                byte[] buffer = new byte[encryptedBytes.Length + 4];
                PackHelper.PackInt(encryptedBytes.Length, buffer, ref offset);  // 长度

                Buffer.BlockCopy(encryptedBytes, 0, buffer, offset, encryptedBytes.Length);

                // 发送
                stream.Write(buffer, 0, buffer.Length);
                Debug.Log("Send AES key to server");
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
                catch(Exception e)
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
                catch(Exception e)
                {
                    Debug.LogError($"Decrypt Error: {e}");
                    return null;
                }
            }
        }
    }
}
