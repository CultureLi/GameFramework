﻿
using System;
using System.Runtime.InteropServices;

namespace Framework
{
    /// <summary>
    /// 对象信息。
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public struct ObjectInfo
    {
        private readonly string _name;
        private readonly bool _locked;
        private readonly bool _customCanReleaseFlag;
        private readonly int _priority;
        private readonly DateTime _lastUseTime;
        private readonly int _spawnCount;

        /// <summary>
        /// 初始化对象信息的新实例。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <param name="locked">对象是否被加锁。</param>
        /// <param name="customCanReleaseFlag">对象自定义释放检查标记。</param>
        /// <param name="priority">对象的优先级。</param>
        /// <param name="lastUseTime">对象上次使用时间。</param>
        /// <param name="spawnCount">对象的获取计数。</param>
        public ObjectInfo(string name, bool locked, bool customCanReleaseFlag, int priority, DateTime lastUseTime, int spawnCount)
        {
            this._name = name;
            this._locked = locked;
            this._customCanReleaseFlag = customCanReleaseFlag;
            this._priority = priority;
            this._lastUseTime = lastUseTime;
            this._spawnCount = spawnCount;
        }

        /// <summary>
        /// 获取对象名称。
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
        }

        /// <summary>
        /// 获取对象是否被加锁。
        /// </summary>
        public bool Locked
        {
            get
            {
                return _locked;
            }
        }

        /// <summary>
        /// 获取对象自定义释放检查标记。
        /// </summary>
        public bool CustomCanReleaseFlag
        {
            get
            {
                return _customCanReleaseFlag;
            }
        }

        /// <summary>
        /// 获取对象的优先级。
        /// </summary>
        public int Priority
        {
            get
            {
                return _priority;
            }
        }

        /// <summary>
        /// 获取对象上次使用时间。
        /// </summary>
        public DateTime LastUseTime
        {
            get
            {
                return _lastUseTime;
            }
        }

        /// <summary>
        /// 获取对象是否正在使用。
        /// </summary>
        public bool IsInUse
        {
            get
            {
                return _spawnCount > 0;
            }
        }

        /// <summary>
        /// 获取对象的获取计数。
        /// </summary>
        public int SpawnCount
        {
            get
            {
                return _spawnCount;
            }
        }
    }
}
