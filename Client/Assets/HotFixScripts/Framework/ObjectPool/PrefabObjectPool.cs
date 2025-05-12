using System;
using UnityEngine;

namespace Framework
{
    public partial class PrefabObjectPool
    {
        private IObjectPoolManager _objectPoolMgr;
        private IResourceMgr _resourceMgr;
        private IObjectPool<PrefabObject> _pool;

        public PrefabObjectPool(string name, int capacity, float expireTime)
        {
            _objectPoolMgr = FrameworkMgr.GetModule<IObjectPoolManager>();
            _resourceMgr = FrameworkMgr.GetModule<IResourceMgr>();
            _pool = _objectPoolMgr.CreateSingleSpawnObjectPool<PrefabObject>(name, capacity, expireTime, 1);
        }

        public int Count => _pool.Count;

        public GameObject Spawn(string location)
        {
            var obj = _pool.Spawn(location);
            if (obj != null)
            {
                return obj.Target as GameObject;
            }

            var handler = _resourceMgr.InstantiateAsync(location);
            handler.WaitForCompletion();

            obj = PrefabObject.Create(location, handler.Result);
            _pool.Register(obj, true);

            return handler.Result;
        }

        public void SpawnAsync(string location, Action<GameObject> cb)
        {
            var obj = _pool.Spawn(location);
            if (obj != null)
            {
                cb?.Invoke(obj.Target as GameObject);
                return;
            }

            var handler = _resourceMgr.InstantiateAsync(location);
            handler.Completed += (handler) =>
            {
                obj = PrefabObject.Create(location, handler.Result);
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
