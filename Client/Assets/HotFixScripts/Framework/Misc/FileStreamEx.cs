using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Networking;

namespace Framework
{
    public class FileStreamEx : Stream
    {
        public Stream Stream
        {
            get; private set;
        }

        private UnityWebRequest _request;
        private bool _disposed = false;

        public FileStreamEx(string path)
        {
            if (path.IndexOf("://", System.StringComparison.Ordinal) >= 0)
            {
                _request = UnityWebRequest.Get(path);
                _request.SendWebRequest();
                if (!WaitForRequest(_request))
                {
                    throw new IOException($"FileStreamEx 打开失败: {path}, error: {_request.error}");
                }

                var nativeData = _request.downloadHandler.nativeData;
                unsafe
                {
                    //非托管内存（NativeArray，native heap）
                    //Unity 下载数据后，放在 C++native buffer，nativeData 直接是个 NativeArray<byte>，无需拷贝
                    //内存分配和释放更高效，特别适合大文件（音视频流 / Zip / 图片 / 二进制表）
                    //需要手动释放（Dispose）
                    Stream = new UnmanagedMemoryStream((byte*)nativeData.GetUnsafeReadOnlyPtr(), nativeData.Length);
                }
            }
            else
            {
                Stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            }
        }

        /// <summary>
        /// 同步等待 UnityWebRequest 完成。
        /// 原来写的是 while (!isDone); —— 空转占满一个核心，而且请求如果因为平台限制永远不完成，
        /// 主线程会直接卡死。这里加了超时，至少能把错误暴露出来。
        /// </summary>
        private static bool WaitForRequest(UnityWebRequest request, float timeoutSeconds = 30f)
        {
            float deadline = UnityEngine.Time.realtimeSinceStartup + timeoutSeconds;
            while (!request.isDone)
            {
                if (UnityEngine.Time.realtimeSinceStartup > deadline)
                {
                    request.Abort();
                    return false;
                }

                System.Threading.Thread.Sleep(1);
            }

            return request.result == UnityWebRequest.Result.Success;
        }

        public override bool CanRead => Stream.CanRead;
        public override bool CanSeek => Stream.CanSeek;
        public override bool CanWrite => Stream.CanWrite;
        public override long Length => Stream.Length;
        public override long Position
        {
            get => Stream.Position;
            set => Stream.Position = value;
        }

        public override void Flush()
        {
            Stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return Stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            Stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Stream.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Stream?.Dispose();
                    _request?.Dispose();
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        ~FileStreamEx()
        {
            Dispose(false);
        }
    }
}
