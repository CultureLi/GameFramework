using System;
namespace Framework
{
    public class ZipHelper
    {
        /// <summary>
        /// 压缩
        /// </summary>
        /// <param name="srcBuffer"></param>
        /// <param name="srcOffset"></param>
        /// <param name="srcLenth"></param>
        /// <returns></returns>
        public static byte[] Zip(byte[] srcBuffer, int srcOffset, int srcLength)
        {
            var zipedBytes = LZ4.LZ4Codec.Encode(srcBuffer, srcOffset, srcLength);
            var buffer = new byte[zipedBytes.Length + 4];
            //把原始数据长度放在压缩数据的前面
            PackHelper.PackInt(srcLength, buffer);
            Buffer.BlockCopy(zipedBytes, 0, buffer, 4, zipedBytes.Length);
            return buffer;
        }

        /// <summary>
        /// 解压
        /// </summary>
        /// <param name="srcBuffer"></param>
        /// <param name="srcOffset"></param>
        /// <param name="srcLength"></param>
        public static byte[] UnZip(byte[] srcBuffer, int srcOffset, int srcLength)
        {
            //先取原始数据大小
            var originLength = PackHelper.UnPackInt(srcBuffer, srcOffset);
            return LZ4.LZ4Codec.Decode(srcBuffer, srcOffset + 4, srcLength - 4, originLength);
        }
    }
}
