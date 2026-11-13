using System;

namespace Framework
{
    public class PackHelper
    {
        public static void PackByte(byte val, byte[] buf, ref int offset)
        {
            PackByte(val, buf, offset);
            offset += 1;
        }

        public static void PackInt(int val, byte[] buf, ref int offset)
        {
            PackInt(val, buf, offset);
            offset += 4;
        }

        public static void PackByte(byte val, byte[] buf, int offset = 0)
        {
            buf[offset] = val;
        }

        /// <summary>
        /// 大端写入 int。直接按位写，避免 BitConverter.GetBytes 每条消息产生的临时数组。
        /// </summary>
        public static void PackInt(int val, byte[] buf, int offset = 0)
        {
            buf[offset] = (byte)(val >> 24);
            buf[offset + 1] = (byte)(val >> 16);
            buf[offset + 2] = (byte)(val >> 8);
            buf[offset + 3] = (byte)val;
        }

        public static byte UnPackByte(byte[] buf, ref int offset)
        {
            byte val = UnPackByte(buf, offset);
            offset += 1;
            return val;
        }

        public static int UnPackInt(byte[] buf, ref int offset)
        {
            int val = UnPackInt(buf, offset);
            offset += 4;
            return val;
        }

        public static byte UnPackByte(byte[] buf, int offset = 0)
        {
            byte val = buf[offset];
            return val;
        }

        public static int UnPackInt(byte[] buf, int offset = 0)
        {
            return (buf[offset] << 24)
                   | (buf[offset + 1] << 16)
                   | (buf[offset + 2] << 8)
                   | buf[offset + 3];
        }
    }
}
