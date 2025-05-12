using Framework;
using GameEntry;
using System;
using UnityEngine;

namespace Test.Runtime.ObjectPoolTest
{
    public partial class PrefabObjectPool
    {
        private IObjectPool<PrefabObject> _pool;

        public PrefabObjectPool(string name, int capacity, float expireTime)
        {
            _pool = FW.ObjectPoolMgr.CreateSingleSpawnObjectPool<PrefabObject>(name, capacity, expireTime, 1);
        }

        public int Count => _pool.Count;

        public GameObject Spawn(string location)
        {
            var obj = _pool.Spawn(location);
            if (obj != null)
            {
                return obj.Target as GameObject;
            }

            var handler = FW.ResourceMgr.InstantiateAsync(location);
            handler.WaitForCompletion();

            obj = PrefabObject.Create(handler.Result);
            _pool.Register(obj, true);

            return handler.Result;
        }

        public void SpawnAsync(string location, Action<GameObject> cb)
        {
            var obj = _pool.Spawn(location);
            if (obj == null)
            {
                cb?.Invoke(obj.Target as GameObject);
                return;
            }

            var handler = FW.ResourceMgr.InstantiateAsync(location);
            handler.Completed += (handler) =>
            {
                obj = PrefabObject.Create(handler.Result);
                _pool.Register(obj, true);
                cb?.Invoke(obj.Target as GameObject);
            };
        }

        public void UnSpawn(GameObject go)
        {
            _pool.Unspawn(go);
        }
    }
}
