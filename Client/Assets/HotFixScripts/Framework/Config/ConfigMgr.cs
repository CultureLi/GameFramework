﻿using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Framework
{

    public abstract class TableBase
    {
        public virtual bool UseOffset
        {
            get; private set;
        }
        public abstract void Initialize(ByteBuf buf);
    }

    public class ConfigMgr : IConfigMgr, IFramework
    {
        IResourceMgr _resourceMgr;
        System.Func<string, ByteBuf> _loader;

        Dictionary<Type, TableBase> _tableMap;

        public ConfigMgr()
        {
            _resourceMgr = FrameworkMgr.GetModule<IResourceMgr>();
            _loader = new System.Func<string, ByteBuf>(LoadByteBuf);
            _tableMap = new Dictionary<Type, TableBase>();
        }

        public T GetTable<T>() where T : TableBase, new()
        {
            if (_tableMap.TryGetValue(typeof(T), out var t))
            {
                return t as T;
            }

            var buf = _loader(typeof(T).Name);
            var table = new T();

            if (table.UseOffset)
            {
                
            }

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

            var buf = _loader(fileName);
            var table = new T();
            table.Initialize(buf);
            _tableMap[typeof(T)] = table;
            return table;
        }

        private ByteBuf LoadByteBuf(string file)
        {
            var handle = _resourceMgr.LoadAssetAsync<TextAsset>($"Assets/BundleRes/Config/{file}.bytes");
            handle.WaitForCompletion();
            return new ByteBuf(handle.Result.bytes);
        }

        public void Shutdown()
        {
            _loader = null;
            _tableMap.Clear();
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {

        }
    }
}
