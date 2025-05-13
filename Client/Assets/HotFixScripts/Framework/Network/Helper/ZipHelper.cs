using System;
using K4os.Compression.LZ4;
using UnityEngine;
namespace Framework
{
    public class ZipHelper
    {
        /// <summary>
        /// 压缩
        /// </summary>
        /// <param name="srcBuffer"></param>
        /// <param name="srcLength"></param>
        /// <param name="dstBuffer"></param>
        /// <returns></returns>
        public static int Zip(byte[] srcBuffer, int srcLength, byte[] dstBuffer)
        {
            try
            {
                //把原始数据长度放在压缩数据的前面
                int offset = 0;
                PackHelper.PackInt(srcLength, dstBuffer, ref offset);
                var size = 4;
                size += LZ4Codec.Encode(srcBuffer, 0, srcLength,
                    dstBuffer, offset, dstBuffer.Length - offset);

                return size;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return 0;
            }
            
        }

        /// <summary>
        /// 解压
        /// </summary>
        /// <param name="srcBuffer"></param>
        /// <param name="srcLength"></param>
        /// <param name="dstBuffer"></param>
        public static int UnZip(byte[] srcBuffer, int srcLength, byte[] dstBuffer)
        {
            try
            {
                //先取原始数据大小
                int offset = 0;
                var originLength = PackHelper.UnPackInt(srcBuffer, ref offset);

                if (originLength >= dstBuffer.Length)
                {
                    throw new Exception("UnZip dstBuffer too small");
                }

                return LZ4Codec.Decode(srcBuffer, offset, srcLength - offset, dstBuffer, 0, originLength);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return 0;
            }
        }
    }
}
