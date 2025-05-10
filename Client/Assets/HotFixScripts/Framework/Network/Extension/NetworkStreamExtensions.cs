using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Framework
{
    public static class NetworkStreamExtensions
    {
        public static int ReadSafely(this NetworkStream stream, byte[] buffer, int offset, int size)
        {
            try
            {
                return stream.Read(buffer, offset, size);
            }
            catch (IOException)
            {
                return 0;
            }
        }

        public static bool ReadExactly(this NetworkStream stream, byte[] buffer, int amount)
        {
            int num;
            for (int i = 0; i < amount; i += num)
            {
                int size = amount - i;
                num = stream.ReadSafely(buffer, i, size);
                if (num == 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 完整读取amout个字节，会多次读取
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public static bool ReadCompletely(this NetworkStream stream, byte[] buffer, int amount)
        {
            int totalRead = 0;

            while (totalRead < amount)
            {
                int read = stream.Read(buffer, totalRead, amount - totalRead);
                if (read == 0)
                    return false;
                totalRead += read;
            }

            return true;
        }

    }
}
