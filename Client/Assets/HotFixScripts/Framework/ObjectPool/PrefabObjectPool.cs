using System;
using UnityEngine;

namespace Framework
{
    public partial class PrefabObjectPool : MonoBehaviour
    {
        private static IObjectPoolManager _objectPoolMgr;
        private static IResourceMgr _resourceMgr;
        private IObjectPool<PrefabObject> _pool;
        private string _name;
        public int Count => _pool.Count;

        /// <summary>
        /// 预制体对象池
        /// </summary>
        /// <param name="name"></param>
        /// <param name="capacity"> 池子最大缓存容量 </param>
        /// <param name="expireTime"> 空闲 Object 过期时间, 过期销毁 </param>
        public static PrefabObjectPool Create(string name, int capacity = 20, float expireTime = 60)
        {
            var go = new GameObject(name);
            var comp = go.AddComponent<PrefabObjectPool>();
            _objectPoolMgr = FrameworkMgr.GetModule<IObjectPoolManager>();
            _resourceMgr = FrameworkMgr.GetModule<IResourceMgr>();

            var pool = _objectPoolMgr.CreateSingleSpawnObjectPool<PrefabObject>(name, capacity, expireTime, 1);
            comp.Init(name, pool);

            return comp;
        }

        private void Init(string name, IObjectPool<PrefabObject> pool)
        {
            _name = name;
            _pool = pool;
        }

        private PrefabObject RegisterObject(string key, GameObject go)
        {
            var obj = PrefabObject.Create(key, go, transform);
            _pool.Register(obj, true);
            return obj;
        }

        /// <summary>
        /// 获取对象
        /// </summary>
        /// <param name="location">资源路径</param>
        /// <returns></returns>
        public GameObject Spawn(string location)
        {
            var obj = _pool.Spawn(location);
            if (obj != null)
            {
                return obj.Target as GameObject;
            }

            var handler = _resourceMgr.InstantiateAsync(location);
            handler.WaitForCompletion();

            RegisterObject(location, handler.Result);

            return handler.Result;
        }

        /// <summary>
        /// 获取对象
        /// </summary>
        /// <param name="name"> 名字 </param>
        /// <param name="template"> 模板，会根据模板创建对象 </param>
        /// <returns></returns>
        public GameObject Spawn(string name, GameObject template)
        {
            var obj = _pool.Spawn(name);
            if (obj != null)
            {
                return obj.Target as GameObject;
            }

            var go = Instantiate(template);
            RegisterObject(name, go);

            return go;
        }

        /// <summary>
        /// 异步获取对象
        /// </summary>
        /// <param name="location"></param>
        /// <param name="cb"></param>
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
                var obj = RegisterObject(location, handler.Result);
                cb?.Invoke(obj.Target as GameObject);
            };
        }

        /// <summary>
        /// 回收对象
        /// </summary>
        /// <param name="go"></param>
        public void UnSpawn(GameObject go)
        {
            _pool.Unspawn(go);
        }

        public void OnDestroy()
        {
            _objectPoolMgr.DestroyObjectPool<PrefabObject>(_name);
        }
    }
}
