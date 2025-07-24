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
        public static string ReadAllText(string path)
        {
            if (path.IndexOf("://", StringComparison.Ordinal) >= 0)
            {
                using (var request = UnityWebRequest.Get(path))
                {
                    request.SendWebRequest();
                    while (!request.isDone)
                        ;
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
                    while (!request.isDone)
                        ;
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
