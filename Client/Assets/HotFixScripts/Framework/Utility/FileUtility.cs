using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
