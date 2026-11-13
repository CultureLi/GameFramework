using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Framework
{
    public partial class PrefabObjectPool : IInstanceOwner
    {
        private IObjectPool<PrefabObject> _pool;
        private string _name;

        /// <summary>
        /// instanceId -> 池内包装对象。
        /// 用于外部销毁回调里按 instanceId 反查记录：回调发生在 OnDestroy 期间，
        /// GameObject 已经是假 null，不能再拿它当字典 key 或传给 _pool.Unspawn。
        /// </summary>
        private readonly Dictionary<int, PrefabObject> _instances = new Dictionary<int, PrefabObject>();

        public int Count => _pool.Count;

        /// <summary>池子整体销毁中：此时不能再回调进 _pool 改动它内部字典，由 Shutdown 统一清理。</summary>
        private bool _releaseManually;

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
            if (FrameworkMgr.ObjectPoolMgr.GetObjectPool<PrefabObject>(name) != null)
            {
                Debug.LogError($"PrefabObjectPool Name: {name} is Already Exist !!");
                return null;
            }

            var instance = new PrefabObjectPool();
            instance._name = name;
            instance._poolRoot = new GameObject(name);
            GameObject.DontDestroyOnLoad(instance._poolRoot);
            instance._pool = FrameworkMgr.ObjectPoolMgr.CreateSingleSpawnObjectPool<PrefabObject>(name, expireTime, capacity, expireTime);
            return instance;
        }


        private PrefabObject RegisterObject(string location, AsyncOperationHandle<GameObject> handle)
        {
            var go = handle.Result;
            var obj = PrefabObject.Create(location, handle, _poolRoot.transform);
            var comp = go.GetOrAddComponent<AddressableInstanceHandle>();
            comp.Bind(this, handle);
            _pool.Register(obj, true);
            _instances[go.GetInstanceID()] = obj;
            return obj;
        }

        private PrefabObject RegisterObject(string location, GameObject go)
        {
            var obj = PrefabObject.Create(location, go, _poolRoot.transform);
            var comp = go.GetOrAddComponent<AddressableInstanceHandle>();
            comp.Bind(this);
            _pool.Register(obj, true);
            _instances[go.GetInstanceID()] = obj;
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
                var handler = FrameworkMgr.ResMgr.InstantiateAsync(location);
                handler.WaitForCompletion();
                if (handler.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"PrefabPool Spawn:{location} Failed");
                    // 失败时底层仍然持有一份引用，必须释放，否则永远还不掉；
                    // 同时不能再往下走 RegisterObject —— handler.Result 为 null 会在里面炸。
                    handler.Release();
                    return null;
                }

                obj = RegisterObject(location, handler);
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

            var handler = FrameworkMgr.ResMgr.InstantiateAsync(location);
            handler.AddCompleted((handler) =>
            {
                if (handler.Status == AsyncOperationStatus.Succeeded)
                {
                    var obj = RegisterObject(location, handler);
                    cb?.Invoke(obj.Target as GameObject);
                }
                else
                {
                    Debug.LogError($"PrefabPool SpawnAsync:{location} Failed");
                    cb?.Invoke(null);
                    handler.Release();
                }

            });
        }

        /// <summary>
        /// 回收对象
        /// </summary>
        /// <param name="go"></param>
        public void UnSpawn(GameObject go)
        {
            // 已经被外部销毁的对象在这里已经是 Unity 假 null，
            // 直接传给 _pool.Unspawn 会在其开头的 target == null 检查里抛异常。
            // 这种情况 OnInstanceDestroyed 已经在销毁回调里把记录摘掉了，这里直接忽略即可。
            if (go == null)
                return;

            if (!_instances.ContainsKey(go.GetInstanceID()))
            {
                Debug.LogWarning($"[PrefabObjectPool:{_name}] 对象不属于本池或已被销毁: {go.name}");
                return;
            }

            _pool.Unspawn(go);
        }

        /// <summary>
        /// prefab 实例被外部销毁（父节点连带销毁、场景卸载、业务直接 Destroy）时的回调。
        /// 由 <see cref="AddressableInstanceHandle.OnDestroy"/> 触发，此时 GameObject 已经是假 null，
        /// 所以只按 instanceId 操作，不碰 GameObject 引用。
        ///
        /// 这里只负责"把池子里的记录摘掉"：
        ///   - Addressables 句柄由 AddressableInstanceHandle 自己释放（已经先于本回调完成/或随后完成）
        ///   - 记录摘掉后，这个实例不会再被 Spawn 取到，也不会被容量/过期逻辑当成有效缓存
        /// </summary>
        public void OnInstanceDestroyed(int instanceId)
        {
            if (!_instances.TryGetValue(instanceId, out var obj))
            {
                // 已经被移除过（比如池子淘汰流程里先摘了记录再触发 OnDestroy），直接忽略。
                return;
            }

            _instances.Remove(instanceId);

            // 池子整体销毁中：_pool 内部正在遍历自身字典，此时不能再调用 ForceRemoveObject 改动它，
            // 交给 ObjectPool<T>.Shutdown() 统一清理。
            if (_releaseManually)
                return;

            if (obj == null)
                return;

            var target = obj.Target;
            if (ReferenceEquals(target, null))
                return;

            // 用 ForceRemoveObject：对象已经销毁，不能用 Unspawn/ReleaseObject 那条带假 null 检查的路径。
            // 移除时会走 PrefabObject.Release()，其中的 GameObject 空判断保证不会去碰已销毁对象。
            _pool.ForceRemoveObject(target);
        }

        public void Dispose()
        {
            _releaseManually = true;
            FrameworkMgr.ObjectPoolMgr.DestroyObjectPool<PrefabObject>(_name);
            _releaseManually = false;

            _instances.Clear();

            if (_poolRoot != null)
            {
                GameObject.Destroy(_poolRoot);
                _poolRoot = null;
            }
        }

        public void SetCanBeReleased(GameObject go, bool canBeReleased)
        {
            if (go == null)
                return;

            _pool.SetCanBeReleased(go, canBeReleased);
        }
    }
}
