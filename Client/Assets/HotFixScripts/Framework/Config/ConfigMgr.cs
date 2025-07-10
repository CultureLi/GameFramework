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
        public virtual void Initialize(string name, Func<string, MemoryStream> streamLoader, System.Func<string, int, int, ByteBuf> byteBufLoader)
        {
        }
        public virtual void Initialize(string name, Func<string, MemoryStream> streamLoader)
        {
        }
    }

    public class ConfigMgr : IConfigMgr, IFramework
    {
        private static byte[] BUFFER = new byte[1024 * 1024 * 5]; //来个5MB
        Dictionary<Type, TableBase> _tableMap;
        private Dictionary<string, ZipArchive> _zipArchiveMap;
        private Dictionary<string, MemoryStream> _tableStreamMap;
        public ConfigMgr()
        {
            _zipArchiveMap = new Dictionary<string, ZipArchive>();
            _tableMap = new Dictionary<Type, TableBase>();
            _tableStreamMap = new Dictionary<string, MemoryStream>();
        }

        public void AddZipArchive(string name, ZipArchive archive)
        {
            _zipArchiveMap[name] = archive;
        }

        public T GetTable<T>(string customFileName = null) where T : TableBase, new()
        {
            if (_tableMap.TryGetValue(typeof(T), out var t))
            {
                return t as T;
            }

            var table = new T();
            var fileName = string.IsNullOrEmpty(customFileName) ? typeof(T).Name : customFileName;

            if (table.UseOffset)
            {
                table.Initialize(fileName, TableStreamLoader, OffsetByteBufLoader);
            }
            else
            {
                table.Initialize(fileName, TableStreamLoader);
            }
            _tableMap[typeof(T)] = table;
            return table;
        }

        MemoryStream TableStreamLoader(string name)
        {
            if (_tableStreamMap.TryGetValue(name, out var stream))
            {
                return stream;
            }

            foreach ((var _, var zipArchive) in _zipArchiveMap)
            {
                var entry = zipArchive.GetEntry(name);
                if (entry != null)
                {
                    stream = new MemoryStream(new byte[entry.Length]); 
                    using (var entryStream = entry.Open()) 
                    {
                        entryStream.CopyTo(stream);
                        _tableStreamMap[name] = stream;
                        return stream;
                    }
                }
            }
            return null;
        }

        private ByteBuf OffsetByteBufLoader(string file, int offset, int length)
        {
            var stream = TableStreamLoader(file);
            stream.Seek(offset, SeekOrigin.Begin);
            stream.Read(BUFFER, 0, length);
            var buf = new ByteBuf(BUFFER, 0, length);
            return buf;
        }

        public void Shutdown()
        {
            foreach ((var _, var zipArchive) in _zipArchiveMap)
            {
                zipArchive.Dispose();
            }
            _zipArchiveMap.Clear();

            foreach ((var _, var stream) in _tableStreamMap)
            {
                stream.Dispose();
            }
            _tableStreamMap.Clear();

            _tableMap.Clear();
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {

        }
    }
}
