using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Framework
{
    public static class FileUtility
    {
        /// <summary>
        /// 同步等待 UnityWebRequest 完成（带超时）。
        /// 原来的 while (!isDone); 会空转占满一个核心，请求不完成时主线程会卡死。
        /// </summary>
        private static bool WaitForRequest(UnityWebRequest request, float timeoutSeconds = 30f)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!request.isDone)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    request.Abort();
                    return false;
                }

                System.Threading.Thread.Sleep(1);
            }

            return request.result == UnityWebRequest.Result.Success;
        }

        public static string ReadAllText(string path)
        {
            if (path.IndexOf("://", StringComparison.Ordinal) >= 0)
            {
                using (var request = UnityWebRequest.Get(path))
                {
                    request.SendWebRequest();
                    if (!WaitForRequest(request))
                    {
                        Debug.LogError($"FileUtility.ReadAllText failed: {path}, error: {request.error}");
                        return null;
                    }

                    return request.downloadHandler.text;
                }
            }
            else if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
            return null;
        }

        public static string ReadAllText(string[] paths)
        {
            foreach (var path in paths)
            {
                var text = ReadAllText(path);
                if (!string.IsNullOrEmpty(text))
                {
                    return text;
                }
            }
            return null;
        }

        public static string ReadAllTextWithRelativePath(string RelativePath)
        {
            var text = ReadAllText(Path.Combine(Application.persistentDataPath, RelativePath));
            if (string.IsNullOrEmpty(text))
            {
                text = ReadAllText(Path.Combine(Application.streamingAssetsPath, RelativePath));
            }
            return text;
        }

        public static byte[] ReadAllBytes(string path)
        {
            if (path.IndexOf("://", StringComparison.Ordinal) >= 0)
            {
                using (var request = UnityWebRequest.Get(path))
                {
                    request.SendWebRequest();
                    if (!WaitForRequest(request))
                    {
                        Debug.LogError($"FileUtility.ReadAllBytes failed: {path}, error: {request.error}");
                        return null;
                    }

                    return request.downloadHandler.data;
                }
            }
            else if (File.Exists(path))
            {
                return File.ReadAllBytes(path);
            }
            return null;
        }

        public static byte[] ReadAllBytes(string[] paths)
        {
            foreach (var path in paths)
            {
                var bytes = ReadAllBytes(path);
                if (bytes != null)
                {
                    return bytes;
                }
            }
            return null;
        }

        public static byte[] ReadAllBytesWithRelativePath(string RelativePath)
        {
            var bytes = ReadAllBytes(Path.Combine(Application.persistentDataPath, RelativePath));
            if (bytes == null)
            {
                bytes = ReadAllBytes(Path.Combine(Application.streamingAssetsPath, RelativePath));
            }
            return bytes;
        }
    }
}
