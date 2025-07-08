using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace Framework
{

    public abstract class TableBase
    {
        public virtual bool UseOffset
        {
            get; private set;
        }
        public virtual void Initialize(ByteBuf buf) { }
        public virtual void Initialize(ByteBuf _buf, System.Func<string, int, int, ByteBuf> byteBufLoader) { }
    }

    public class ConfigMgr : IConfigMgr, IFramework
    {

        Dictionary<Type, TableBase> _tableMap;
        Dictionary<string, FileStream> _fileStreams;
        private static byte[] BUFFER = new byte[1024 * 1024 * 10];

        private Dictionary<string, ZipArchive> _zipArchiveMap = new Dictionary<string, ZipArchive>();
        private Dictionary<string, FileStream> _tableStreamMap = new Dictionary<string, FileStream>();
        public ConfigMgr()
        {
            _tableMap = new Dictionary<Type, TableBase>();
            _fileStreams = new Dictionary<string, FileStream>();
        }

        public void AddZipArchive(string name, ZipArchive archive)
        {
            _zipArchiveMap[name] = archive;
        }

        public T GetTable<T>() where T : TableBase, new()
        {
            if (_tableMap.TryGetValue(typeof(T), out var t))
            {
                return t as T;
            }

            var table = new T();

            var buf = LoadByteBuf(typeof(T).Name);
            table.Initialize(buf);

            _tableMap[typeof(T)] = table;
            return table;
        }

        public T GetTable<T>(string fileName) where T : TableBase, new()
        {
            if (_tableMap.TryGetValue(typeof(T), out var t))
            {
                return t as T;
            }

            var buf = LoadByteBuf(fileName);
            var table = new T();
            table.Initialize(buf);
            _tableMap[typeof(T)] = table;
            return table;
        }

        private ByteBuf LoadByteBuf(string file)
        {
            ByteBuf buf = null;
            

            return buf;
        }

        private ByteBuf OffsetByteBufLoader(string file, int offset, int length)
        {
            if (!_fileStreams.TryGetValue(file, out var fs))
            {
                fs = new FileStream($"{Application.streamingAssetsPath}/Config/bin/{file}.bytes", FileMode.Open);
                _fileStreams.Add(file, fs);
            }
            fs.Seek(offset, SeekOrigin.Begin);
            fs.Read(BUFFER, 0, length);
            var buf = new ByteBuf(BUFFER, 0, length);
            return buf;
        }

        public void Shutdown()
        {
            _tableMap.Clear();
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {

        }
    }
}
