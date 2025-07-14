using AOTBase;
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
            Initialize();
        }

        /// <summary>
        /// 初始化，加载最新的zip文件
        /// </summary>
        public void Initialize()
        {
            Shutdown();
            foreach (var zipName in new string[] { "configData.zip", "i18n.zip" })
            {
                var zipPath = Path.Combine(Application.persistentDataPath, "Config", zipName);
                if (!File.Exists(zipPath))
                {
                    zipPath = Path.Combine(Application.streamingAssetsPath, "Config", zipName);
                }
                var stream = new FileStreamEx(zipPath);

                ZipArchive archive = new ZipArchive(stream.Stream, ZipArchiveMode.Read);

                AddZipArchive(zipName, archive);
            }
        }

        void AddZipArchive(string name, ZipArchive archive)
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

        /// <summary>
        /// 加载配置表数据流
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
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
        /// <summary>
        /// 加载流中某条数据
        /// </summary>
        /// <param name="file"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
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
