﻿using System;

namespace Framework
{
    internal sealed partial class ObjectPoolMgr : IFramework, IObjectPoolMgr
    {
        /// <summary>
        /// 内部对象。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        private sealed class Object<T> : IReference where T : ObjectBase
        {
            private T _object;
            private int _spawnCount;

            /// <summary>
            /// 初始化内部对象的新实例。
            /// </summary>
            public Object()
            {
                _object = null;
                _spawnCount = 0;
            }

            /// <summary>
            /// 获取对象名称。
            /// </summary>
            public string Name
            {
                get
                {
                    return _object.Name;
                }
            }

            /// <summary>
            /// 获取自定义释放检查标记。
            /// </summary>
            public bool CustomCanReleaseFlag
            {
                get
                {
                    return _object.CustomCanReleaseFlag;
                }
            }

            /// <summary>
            /// 获取对象上次使用时间。
            /// </summary>
            public DateTime LastUseTime
            {
                get
                {
                    return _object.LastUseTime;
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

            /// <summary>
            /// 创建内部对象。
            /// </summary>
            /// <param name="obj">对象。</param>
            /// <param name="spawned">对象是否已被获取。</param>
            /// <returns>创建的内部对象。</returns>
            public static Object<T> Create(T obj, bool spawned)
            {
                if (obj == null)
                {
                    throw new Exception("Object is invalid.");
                }

                Object<T> internalObject = ReferencePool.Acquire<Object<T>>();
                internalObject._object = obj;
                internalObject._spawnCount = spawned ? 1 : 0;
                if (spawned)
                {
                    obj.OnSpawn();
                }

                return internalObject;
            }

            /// <summary>
            /// 清理内部对象。
            /// </summary>
            public void Clear()
            {
                _object = null;
                _spawnCount = 0;
            }

            /// <summary>
            /// 查看对象。
            /// </summary>
            /// <returns>对象。</returns>
            public T Peek()
            {
                return _object;
            }

            /// <summary>
            /// 获取对象。
            /// </summary>
            /// <returns>对象。</returns>
            public T Spawn()
            {
                _spawnCount++;
                _object.LastUseTime = DateTime.UtcNow;
                _object.OnSpawn();
                return _object;
            }

            /// <summary>
            /// 回收对象。
            /// </summary>
            public void Unspawn()
            {
                _object.OnUnspawn();
                _object.LastUseTime = DateTime.UtcNow;
                _spawnCount--;
                if (_spawnCount < 0)
                {
                    throw new Exception($"Object '{Name}' spawn count is less than 0.");
                }
            }

            /// <summary>
            /// 释放对象。
            /// </summary>
            /// <param name="isShutdown">是否是关闭对象池时触发。</param>
            public void Release(bool isShutdown)
            {
                _object.Release(isShutdown);
                ReferencePool.Release(_object);
            }
        }
    }
}
