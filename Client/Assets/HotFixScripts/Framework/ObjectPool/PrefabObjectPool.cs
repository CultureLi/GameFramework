using System;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Framework
{
    public partial class PrefabObjectPool
    {
        private static IObjectPoolMgr _objectPoolMgr;
        private static IResourceMgr _resourceMgr;
        private IObjectPool<PrefabObject> _pool;
        private string _name;
        public int Count => _pool.Count;

        private GameObject _poolRoot;

        /// <summary>
        /// 构造函数设置为私有，使用 PrefabObjectPool.Create创建
        /// </summary>
        private PrefabObjectPool()
        {
        }

        /// <summary>
        /// 预制体对象池
        /// </summary>
        /// <param name="name"></param>
        /// <param name="capacity"> 池子最大缓存容量 </param>
        /// <param name="expireTime"> 空闲 Object 过期时间, 过期销毁 </param>
        public static PrefabObjectPool Create(string name, int capacity = 10, float expireTime = 20)
        {
            InitMgr();

            if (_objectPoolMgr.GetObjectPool<PrefabObject>(name) != null)
            {
                Debug.LogError($"PrefabObjectPool Name: {name} is Already Exist !!");
                return null;
            }

            var instance = new PrefabObjectPool();
            instance._poolRoot = new GameObject(name);
            GameObject.DontDestroyOnLoad(instance._poolRoot);
            instance._pool = _objectPoolMgr.CreateSingleSpawnObjectPool<PrefabObject>(name, expireTime, capacity, expireTime);
            return instance;
        }

        static void InitMgr()
        {
            if (_objectPoolMgr == null)
            {
                _objectPoolMgr = FrameworkMgr.GetModule<IObjectPoolMgr>();
            }
            if (_resourceMgr == null)
            {
                _resourceMgr = FrameworkMgr.GetModule<IResourceMgr>();
            }
        }

        private PrefabObject RegisterObject(string key, GameObject go)
        {
            var obj = PrefabObject.Create(key, go, _poolRoot.transform, OnPreReleaseObject);
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
            if (obj == null)
            {
                var handler = _resourceMgr.InstantiateAsync(location);
                handler.WaitForCompletion();
                if (handler.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"PrefabPool Spawn:{location} Failed");
                }

                obj = RegisterObject(location, handler.Result);
            }

            return obj.Target as GameObject;
        }

        /// <summary>
        /// 获取对象
        /// </summary>
        /// <param name="name"> 名字 </param>
        /// <param name="template"> 模板，会根据模板创建对象 </param>
        /// <returns></returns>
        public GameObject Spawn(string name, GameObject template)
        {
            if (template == null)
            {
                Debug.LogError($"PrefabPool Spawn:{name}, Template is Null");
                return null;
            }
            var obj = _pool.Spawn(name);
            if (obj == null)
            {
                var go = GameObject.Instantiate(template);
                obj = RegisterObject(name, go);
            }

            return obj.Target as GameObject;
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
            handler.AddCompleted((handler) =>
            {
                if (handler.Status == AsyncOperationStatus.Succeeded)
                {
                    var obj = RegisterObject(location, handler.Result);
                    cb?.Invoke(obj.Target as GameObject);
                }
                else
                {
                    Debug.LogError($"PrefabPool SpawnAsync:{location} Failed");
                    cb?.Invoke(null);
                }
                
            });
        }

        /// <summary>
        /// 回收对象
        /// </summary>
        /// <param name="go"></param>
        public void UnSpawn(GameObject go)
        {
            _pool.Unspawn(go);
        }

        public void UnSpawn(PrefabObject obj)
        {
            _pool.Unspawn(obj);
        }

        void OnPreReleaseObject(GameObject go)
        {
            _resourceMgr.ReleaseInstance(go);
        }

        public void Dispose()
        {
            _objectPoolMgr.DestroyObjectPool<PrefabObject>(_name);
        }

        public void SetCanBeReleased(GameObject go, bool canBeReleased)
        {
            _pool.SetCanBeReleased(go, canBeReleased);
        }
    }
}
