using System;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Framework
{
    /// <summary>
    /// 挂在 Addressables 实例化出来的 GameObject 上，保证对应的句柄无论从哪条路径被销毁，
    /// 都会被释放【恰好一次】——不会漏释放（引用计数永远归不了零，bundle 卸不掉），
    /// 也不会重复释放（对一个已经归零的句柄再调 Release 会抛异常）。
    ///
    /// 会触发释放的路径都覆盖到了：
    ///   1. 业务代码调用 <see cref="AddressableInstantiateHelper.DestroyInstance"/> 主动销毁；
    ///   2. 业务代码直接调用了 UnityEngine.Object.Destroy(go)，没走上面的入口；
    ///   3. 父节点被销毁，子节点被 Unity 连带销毁；
    ///   4. 场景被卸载，场景里的对象被 Unity 自动销毁。
    /// 2/3/4 都不会经过 Addressables 自己的释放流程，必须靠这个组件的 OnDestroy 补上，
    /// 否则这些路径销毁的实例，Addressables 侧的引用计数永远不会减，对应 bundle 永远卸不掉。
    ///
    /// 业务代码不应该直接 new/AddComponent 这个类型，统一通过 <see cref="AddressableInstantiateHelper"/> 使用。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")] // 不出现在 Add Component 菜单里，只能通过代码添加
    public class AddressableInstanceHandle : MonoBehaviour
    {
        IInstanceOwner _owner;
        private AsyncOperationHandle<GameObject> _handle;
        protected bool released;
        private int _instanceId;

        // <summary>实例化完成后绑定对应句柄。只应该由 AddressableInstantiateHelper 调用。</summary>
        public void Bind(AsyncOperationHandle<GameObject> handle)
        {
            _instanceId = gameObject.GetInstanceID();
            _handle = handle;
            released = false;
        }


        public void Bind(IInstanceOwner owner)
        {
            _instanceId = gameObject.GetInstanceID();
            _owner = owner;
            released = false;
        }

        public void Bind(IInstanceOwner owner, AsyncOperationHandle<GameObject> handle)
        {
            _instanceId = gameObject.GetInstanceID();
            _owner = owner;
            _handle = handle;
            released = false;
        }

        /// <summary>
        /// 业务显式销毁这个实例时调用：释放一次句柄（Addressables 会把 GameObject 一并销毁），
        /// 并把 <see cref="released"/> 标记置上。随之触发的 OnDestroy 发现已经释放过，会直接跳过，
        /// 不会对同一个句柄释放第二次。
        /// </summary>
        internal void ReleaseNow()
        {
            if (released)
                return;

            released = true;

            // 主动释放路径也要通知 owner：ReleaseInstance 会销毁 GameObject，但 OnDestroy
            // 触发时 released 已经是 true，会被跳过，不在这里通知的话 owner 的记录就清不掉。
            _owner?.OnInstanceDestroyed(_instanceId);
            ReleaseHandleOnce();
        }

        /// <summary>
        /// 安全网：对象从 ReleaseNow() 之外的任何路径被销毁时，这里补上释放。
        /// 如果是 ReleaseNow() 触发的销毁，released 已经是 true，直接跳过。
        /// </summary>
        protected void OnDestroy()
        {
            if (released)
                return;

            released = true;
            _owner?.OnInstanceDestroyed(_instanceId);
            ReleaseHandleOnce();
        }

        private void ReleaseHandleOnce()
        {
            if (!_handle.IsValid())
                return;

            // Framework 是底层程序集，不能反向依赖 GameEntry.FW；统一走 FrameworkMgr 的缓存属性。
            var resMgr = FrameworkMgr.ResMgr;
            if (resMgr == null)
            {
                Debug.LogWarning($"[AddressableInstanceHandle] IResourceMgr 已不可用，无法释放句柄: {gameObject.name}");
                return;
            }

            // ReleaseInstance 和 Release 对 Instantiate 产生的 AsyncOperationHandle<GameObject>
            // 是等价的，都是 handle.Release()；这里用 ReleaseInstance 只是语义上更贴合"这是一个实例句柄"。
            resMgr.ReleaseInstance(_handle);
        }
    }

    /// <summary>
    /// Prefab 实例化的推荐入口：实例化出来的对象自动挂 <see cref="AddressableInstanceHandle"/>，
    /// 对象被销毁时（不管走哪条路径）自动释放对应的 Addressables 句柄。
    ///
    /// 业务代码不应该再直接调用 IResourceMgr.InstantiateAsync / ReleaseInstance，
    /// 统一走这里的 InstantiateAsync / DestroyInstance，避免漏释放或者用错释放姿势。
    ///
    /// 注意：对象池场景通过 <see cref="Bind(IInstanceOwner, AsyncOperationHandle{GameObject})"/>
    /// 绑定 owner，实例被外部销毁时会通知池子摘除记录（见 PrefabObjectPool.OnInstanceDestroyed）；
    /// 独立的非池化实例则用 <see cref="AddressableInstantiateHelper"/> 的入口。
    /// </summary>
    public static class AddressableInstantiateHelper
    {
        /// <summary>Framework 内部统一通过这个取 IResourceMgr（缓存由 FrameworkMgr 负责），不依赖 GameEntry.FW。</summary>
        private static IResourceMgr ResMgr => FrameworkMgr.ResMgr;

        public static AsyncOperationHandle<GameObject> InstantiateAsync(
            string key, Transform parent = null, bool instantiateInWorldSpace = false)
        {
            var handle = ResMgr.InstantiateAsync(key, parent, instantiateInWorldSpace);
            handle.AddCompleted(OnInstantiateCompleted);
            return handle;
        }

        public static AsyncOperationHandle<GameObject> InstantiateAsync(
            string key, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            var handle = ResMgr.InstantiateAsync(key, position, rotation, parent);
            handle.AddCompleted(OnInstantiateCompleted);
            return handle;
        }

        /// <summary>
        /// 主动销毁一个通过这里实例化出来的对象：释放对应句柄，Addressables 会把 GameObject 一并销毁。
        /// 不要先 Destroy(instance) 再调这个——对象销毁后 GetComponent 就拿不到 holder 了。
        /// </summary>
        public static void DestroyInstance(GameObject instance)
        {
            if (instance == null)
                return;

            var holder = instance.GetComponent<AddressableInstanceHandle>();
            if (holder != null)
            {
                holder.ReleaseNow();
                return;
            }

            // 没有挂 holder，说明这个对象不是通过这里实例化出来的（或者已经被销毁过一次），
            // 按普通对象处理，不去动 Addressables。
            Debug.LogWarning($"[AddressableInstantiateHelper] {instance.name} 上没有找到 " +
                              $"AddressableInstanceHandle，按普通对象 Destroy，不会触碰 Addressables 引用计数。");
            UnityEngine.Object.Destroy(instance);
        }

        private static void OnInstantiateCompleted(AsyncOperationHandle<GameObject> handle)
        {
            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Debug.LogError($"[AddressableInstantiateHelper] 实例化失败: {handle.DebugName}");
                // 失败也必须释放：ProvideInstance 失败时底层仍然持有对 prefab 依赖的一份引用计数，
                // 不释放的话这份引用永远还不掉。
                if (handle.IsValid())
                    ResMgr.ReleaseInstance(handle);
                return;
            }

            var holder = handle.Result.GetComponent<AddressableInstanceHandle>();
            if (holder == null)
                holder = handle.Result.AddComponent<AddressableInstanceHandle>();
            holder.Bind(null, handle);
        }
    }
}
