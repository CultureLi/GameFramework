﻿using System;
using System.Collections.Generic;

namespace Framework
{
    internal sealed partial class ObjectPoolMgr : IFramework, IObjectPoolMgr
    {
        /// <summary>
        /// 对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        private sealed class ObjectPool<T> : ObjectPoolBase, IObjectPool<T> where T : ObjectBase
        {
            private readonly Dictionary<string, LinkedList<Object<T>>> _objects;
            private readonly Dictionary<UnityEngine.Object, Object<T>> _objectMap;
            private readonly ReleaseObjectFilterCallback<T> _defaultReleaseObjectFilterCallback;
            private readonly List<T> _cachedCanReleaseObjects;
            private readonly List<T> _cachedToReleaseObjects;
            private readonly bool _allowMultiSpawn;
            private float _autoReleaseInterval;
            private int _capacity;
            private float _expireTime;
            private float _autoReleaseTime;

            /// <summary>
            /// 预释放对象
            /// </summary>
            public Action<T> PreReleaseObject
            {
                get;set;
            }

            /// <summary>
            /// 初始化对象池的新实例。
            /// </summary>
            /// <param name="name">对象池名称。</param>
            /// <param name="allowMultiSpawn">是否允许对象被多次获取。</param>
            /// <param name="autoReleaseInterval">对象池自动释放可释放对象的间隔秒数。</param>
            /// <param name="capacity">对象池的容量。</param>
            /// <param name="expireTime">对象池对象过期秒数。</param>
            /// <param name="priority">对象池的优先级。</param>
            public ObjectPool(string name, bool allowMultiSpawn, float autoReleaseInterval, int capacity, float expireTime)
                : base(name)
            {
                _objects = new Dictionary<string, LinkedList<Object<T>>>();
                _objectMap = new Dictionary<UnityEngine.Object, Object<T>>();
                _defaultReleaseObjectFilterCallback = DefaultReleaseObjectFilterCallback;
                _cachedCanReleaseObjects = new List<T>();
                _cachedToReleaseObjects = new List<T>();
                _allowMultiSpawn = allowMultiSpawn;
                _autoReleaseInterval = autoReleaseInterval;
                Capacity = capacity;
                ExpireTime = expireTime;
                _autoReleaseTime = 0f;
            }

            /// <summary>
            /// 获取对象池对象类型。
            /// </summary>
            public override Type ObjectType
            {
                get
                {
                    return typeof(T);
                }
            }

            /// <summary>
            /// 获取对象池中对象的数量。
            /// </summary>
            public override int Count
            {
                get
                {
                    return _objectMap.Count;
                }
            }

            /// <summary>
            /// 获取对象池中能被释放的对象的数量。
            /// </summary>
            public override int CanReleaseCount
            {
                get
                {
                    GetCanReleaseObjects(_cachedCanReleaseObjects);
                    return _cachedCanReleaseObjects.Count;
                }
            }

            /// <summary>
            /// 获取是否允许对象被多次获取。
            /// </summary>
            public override bool AllowMultiSpawn
            {
                get
                {
                    return _allowMultiSpawn;
                }
            }

            /// <summary>
            /// 获取或设置对象池自动释放可释放对象的间隔秒数。
            /// </summary>
            public override float AutoReleaseInterval
            {
                get
                {
                    return _autoReleaseInterval;
                }
                set
                {
                    _autoReleaseInterval = value;
                }
            }

            /// <summary>
            /// 获取或设置对象池的容量。
            /// </summary>
            public override int Capacity
            {
                get
                {
                    return _capacity;
                }
                set
                {
                    if (value < 0)
                    {
                        throw new Exception("Capacity is invalid.");
                    }

                    if (_capacity == value)
                    {
                        return;
                    }

                    _capacity = value;
                    Release();
                }
            }

            /// <summary>
            /// 获取或设置对象池对象过期秒数。
            /// </summary>
            public override float ExpireTime
            {
                get
                {
                    return _expireTime;
                }

                set
                {
                    if (value < 0f)
                    {
                        throw new Exception("ExpireTime is invalid.");
                    }

                    if (ExpireTime == value)
                    {
                        return;
                    }

                    _expireTime = value;
                    Release();
                }
            }

            /// <summary>
            /// 创建对象。
            /// </summary>
            /// <param name="obj">对象。</param>
            /// <param name="spawned">对象是否已被获取。</param>
            public void Register(T obj, bool spawned)
            {
                if (obj == null)
                {
                    throw new Exception("Object is invalid.");
                }

                Object<T> internalObject = Object<T>.Create(obj, spawned);

                if (!_objects.TryGetValue(obj.Name, out var objectlist))
                {
                    objectlist = new LinkedList<Object<T>>();
                    _objects[obj.Name] = objectlist;
                }

                objectlist.AddLast(internalObject);
                _objectMap.Add(obj.Target, internalObject);

                if (Count > _capacity)
                {
                    Release();
                }
            }

            /// <summary>
            /// 检查对象。
            /// </summary>
            /// <returns>要检查的对象是否存在。</returns>
            public bool CanSpawn()
            {
                return CanSpawn(string.Empty);
            }

            /// <summary>
            /// 检查对象。
            /// </summary>
            /// <param name="name">对象名称。</param>
            /// <returns>要检查的对象是否存在。</returns>
            public bool CanSpawn(string name)
            {
                if (name == null)
                {
                    throw new Exception("Name is invalid.");
                }

                if (_objects.TryGetValue(name, out var objectList))
                {
                    foreach (Object<T> internalObject in objectList)
                    {
                        if (_allowMultiSpawn || !internalObject.IsInUse)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            /// <summary>
            /// 获取对象。
            /// </summary>
            /// <returns>要获取的对象。</returns>
            public T Spawn()
            {
                return Spawn(string.Empty);
            }

            /// <summary>
            /// 获取对象。
            /// </summary>
            /// <param name="name">对象名称。</param>
            /// <returns>要获取的对象。</returns>
            public T Spawn(string name)
            {
                if (name == null)
                {
                    throw new Exception("Name is invalid.");
                }

                if (_objects.TryGetValue(name, out var objectList))
                {
                    foreach (Object<T> internalObject in objectList)
                    {
                        if (_allowMultiSpawn || !internalObject.IsInUse)
                        {
                            return internalObject.Spawn();
                        }
                    }
                }

                return null;
            }

            /// <summary>
            /// 回收对象。
            /// </summary>
            /// <param name="obj">要回收的对象。</param>
            public void Unspawn(T obj)
            {
                if (obj == null)
                {
                    throw new Exception("Object is invalid.");
                }

                Unspawn(obj.Target);
            }

            /// <summary>
            /// 回收对象。
            /// </summary>
            /// <param name="target">要回收的对象。</param>
            public void Unspawn(UnityEngine.Object target)
            {
                if (target == null)
                {
                    throw new Exception("Target is invalid.");
                }

                Object<T> internalObject = GetObject(target);
                if (internalObject != null)
                {
                    internalObject.Unspawn();
                    if (Count > _capacity && internalObject.SpawnCount <= 0)
                    {
                        Release();
                    }
                }
                else
                {
                    throw new Exception($"Can not find target in object pool '{new TypeNamePair(typeof(T), Name)}', target type is '{target.GetType().FullName}', target value is '{target}'.");
                }
            }

            /// <summary>
            /// 释放对象。
            /// </summary>
            /// <param name="obj">要释放的对象。</param>
            /// <returns>释放对象是否成功。</returns>
            public bool ReleaseObject(T obj)
            {
                if (obj == null)
                {
                    throw new Exception("Object is invalid.");
                }

                return ReleaseObject(obj.Target);
            }

            /// <summary>
            /// 释放对象。
            /// </summary>
            /// <param name="target">要释放的对象。</param>
            /// <returns>释放对象是否成功。</returns>
            public bool ReleaseObject(UnityEngine.Object target)
            {
                if (target == null)
                {
                    throw new Exception("Target is invalid.");
                }

                Object<T> internalObject = GetObject(target);
                if (internalObject == null)
                {
                    return false;
                }

                if (internalObject.IsInUse || !internalObject.CustomCanReleaseFlag)
                {
                    return false;
                }

                if (!_objects.TryGetValue(internalObject.Name, out var objectList))
                {
                    return false;
                }
                objectList.Remove(internalObject);
                _objectMap.Remove(internalObject.Peek().Target);

                internalObject.Release(false);
                ReferencePool.Release(internalObject);
                return true;
            }

            /// <summary>
            /// 释放对象池中的可释放对象。
            /// </summary>
            public override void Release()
            {
                Release(Count - _capacity, _defaultReleaseObjectFilterCallback);
            }

            /// <summary>
            /// 释放对象池中的可释放对象。
            /// </summary>
            /// <param name="toReleaseCount">尝试释放对象数量。</param>
            public override void Release(int toReleaseCount)
            {
                Release(toReleaseCount, _defaultReleaseObjectFilterCallback);
            }

            /// <summary>
            /// 释放对象池中的可释放对象。
            /// </summary>
            /// <param name="releaseObjectFilterCallback">释放对象筛选函数。</param>
            public void Release(ReleaseObjectFilterCallback<T> releaseObjectFilterCallback)
            {
                Release(Count - _capacity, releaseObjectFilterCallback);
            }

            /// <summary>
            /// 释放对象池中的可释放对象。
            /// </summary>
            /// <param name="toReleaseCount">尝试释放对象数量。</param>
            /// <param name="releaseObjectFilterCallback">释放对象筛选函数。</param>
            public void Release(int toReleaseCount, ReleaseObjectFilterCallback<T> releaseObjectFilterCallback)
            {
                if (releaseObjectFilterCallback == null)
                {
                    throw new Exception("Release object filter callback is invalid.");
                }

                if (toReleaseCount < 0)
                {
                    toReleaseCount = 0;
                }

                DateTime expireTime = DateTime.MinValue;
                if (_expireTime < float.MaxValue)
                {
                    expireTime = DateTime.UtcNow.AddSeconds(-_expireTime);
                }

                _autoReleaseTime = 0f;
                GetCanReleaseObjects(_cachedCanReleaseObjects);
                List<T> toReleaseObjects = releaseObjectFilterCallback(_cachedCanReleaseObjects, toReleaseCount, expireTime);
                if (toReleaseObjects == null || toReleaseObjects.Count <= 0)
                {
                    return;
                }

                foreach (T toReleaseObject in toReleaseObjects)
                {
                    ReleaseObject(toReleaseObject);
                }
            }

            /// <summary>
            /// 释放对象池中的所有未使用对象。
            /// </summary>
            public override void ReleaseAllUnused()
            {
                _autoReleaseTime = 0f;
                GetCanReleaseObjects(_cachedCanReleaseObjects);
                foreach (T toReleaseObject in _cachedCanReleaseObjects)
                {
                    ReleaseObject(toReleaseObject);
                }
            }

            public override void Update(float elapseSeconds, float realElapseSeconds)
            {
                _autoReleaseTime += realElapseSeconds;
                if (_autoReleaseTime < _autoReleaseInterval)
                {
                    return;
                }

                Release();
            }

            public override void Shutdown()
            {
                foreach ((var key, var value) in _objectMap)
                {
                    value.Release(true);
                    ReferencePool.Release(value);
                }

                _objects.Clear();
                _objectMap.Clear();
                _cachedCanReleaseObjects.Clear();
                _cachedToReleaseObjects.Clear();
            }

            private Object<T> GetObject(UnityEngine.Object target)
            {
                if (target == null)
                {
                    throw new Exception("Target is invalid.");
                }

                Object<T> internalObject = null;
                if (_objectMap.TryGetValue(target, out internalObject))
                {
                    return internalObject;
                }

                return null;
            }

            private void GetCanReleaseObjects(List<T> results)
            {
                if (results == null)
                {
                    throw new Exception("Results is invalid.");
                }

                results.Clear();
                foreach ((var key, var value) in _objectMap)
                {
                    if (value.IsInUse || !value.CustomCanReleaseFlag)
                    {
                        continue;
                    }

                    results.Add(value.Peek());
                }
            }

            private List<T> DefaultReleaseObjectFilterCallback(List<T> candidateObjects, int toReleaseCount, DateTime expireTime)
            {
                _cachedToReleaseObjects.Clear();

                if (expireTime > DateTime.MinValue)
                {
                    for (int i = candidateObjects.Count - 1; i >= 0; i--)
                    {
                        if (candidateObjects[i].LastUseTime <= expireTime)
                        {
                            _cachedToReleaseObjects.Add(candidateObjects[i]);
                            candidateObjects.RemoveAt(i);
                            continue;
                        }
                    }

                    toReleaseCount -= _cachedToReleaseObjects.Count;
                }

                for (int i = 0; toReleaseCount > 0 && i < candidateObjects.Count; i++)
                {
                    for (int j = i + 1; j < candidateObjects.Count; j++)
                    {
                        if (candidateObjects[i].LastUseTime > candidateObjects[j].LastUseTime)
                        {
                            T temp = candidateObjects[i];
                            candidateObjects[i] = candidateObjects[j];
                            candidateObjects[j] = temp;
                        }
                    }

                    _cachedToReleaseObjects.Add(candidateObjects[i]);
                    toReleaseCount--;
                }

                return _cachedToReleaseObjects;
            }
        }
    }
}
