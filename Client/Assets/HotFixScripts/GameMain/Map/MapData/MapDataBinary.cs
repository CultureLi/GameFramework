using System;
using System.Collections.Generic;
using System.IO;

namespace GameMain
{
    /// <summary>
    /// 服务端使用的二进制地图数据。
    /// 这个文件刻意不引用 UnityEngine，服务端项目可以直接链接编译。
    ///
    /// 格式（小端）：
    ///   uint32 magic   = 'MAP1'
    ///   int32  version = 1
    ///   int32  count
    ///   count × { int32 id, float posX, float posZ, float sizeX, float sizeZ, float rotationY }
    /// </summary>
    public struct MapBinaryEntry
    {
        public int Id;
        public float PosX;
        public float PosZ;
        public float SizeX;
        public float SizeZ;
        public float RotationY;
    }

    public static class MapDataBinary
    {
        // 小端写入后在文件里正好是 'M' 'A' 'P' '1'
        private const uint Magic = 0x3150414D;
        public const int Version = 1;

        private const int MaxEntryCount = 10_000_000;

        public static void Write(string path, IReadOnlyList<MapBinaryEntry> entries)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("地图数据路径为空", nameof(path));

            int count = entries?.Count ?? 0;

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new BinaryWriter(stream);

            writer.Write(Magic);
            writer.Write(Version);
            writer.Write(count);

            if (entries == null)
                return;

            for (int i = 0; i < count; i++)
            {
                var entry = entries[i];
                writer.Write(entry.Id);
                writer.Write(entry.PosX);
                writer.Write(entry.PosZ);
                writer.Write(entry.SizeX);
                writer.Write(entry.SizeZ);
                writer.Write(entry.RotationY);
            }
        }

        public static List<MapBinaryEntry> Read(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(stream);

            uint magic = reader.ReadUInt32();
            if (magic != Magic)
                throw new InvalidDataException($"地图数据文件头错误: {path}");

            int version = reader.ReadInt32();
            if (version != Version)
                throw new InvalidDataException($"地图数据版本不支持: {version} ({path})");

            int count = reader.ReadInt32();
            if (count < 0 || count > MaxEntryCount)
                throw new InvalidDataException($"地图数据条目数非法: {count} ({path})");

            var entries = new List<MapBinaryEntry>(count);
            for (int i = 0; i < count; i++)
            {
                entries.Add(new MapBinaryEntry
                {
                    Id = reader.ReadInt32(),
                    PosX = reader.ReadSingle(),
                    PosZ = reader.ReadSingle(),
                    SizeX = reader.ReadSingle(),
                    SizeZ = reader.ReadSingle(),
                    RotationY = reader.ReadSingle(),
                });
            }

            return entries;
        }
    }
}
