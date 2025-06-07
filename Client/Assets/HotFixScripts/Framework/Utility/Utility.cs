using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 实用函数集。
    /// </summary>
    public static partial class Utility
    {
        public static string FormatByteSize(ulong byteSize)
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            if (byteSize >= GB)
                return $"{byteSize / (double)GB:F2} GB";
            else if (byteSize >= MB)
                return $"{byteSize / (double)MB:F2} MB";
            else if (byteSize >= KB)
                return $"{byteSize / (double)KB:F2} KB";
            else
                return byteSize + "B";
        }

        public static void Test()
        {
            Debug.Log("hhhhhh");
        }
    }
}
