using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Framework
{
    /// <summary>
    /// 一个逻辑模块（UI界面内、地图模块、战斗模块、某个常驻系统...）在生命周期内加载的所有 Addressables 句柄的
    /// 集中登记处：模块销毁时调一次 <see cref="ReleaseAll"/>，不用在模块内部各处散落 Release 调用，
    /// 也不用担心某个分支 return 早了漏掉释放。
    ///
    /// 和 <see cref="AddressableInstantiateHelper"/>（单个实例跟着自己销毁自动释放）不同：
    /// 这里管理的是"跟着模块走"的一批资源——模块活着的时候它们都该活着，模块销毁时一次性全部释放，
    /// 不需要逐个判断"这个资源现在是不是还有人用"。
    ///
    /// </summary>
    public class AddressableHandleGroup : IDisposable
    {
        private readonly List<AsyncOperationHandle> _handles = new List<AsyncOperationHandle>();
        private bool _disposed;

        /// <summary>模块名，只用于日志，方便定位是哪个模块的句柄没释放干净。</summary>
        public string Name { get; }

        /// <summary>当前登记着多少个还没释放的句柄。</summary>
        public int Count => _handles.Count;

        public AddressableHandleGroup(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 加载一份数据资产（Texture/Material/Sprite/Mesh/ScriptableObject/TextAsset...）并登记到组里。
        /// 返回句柄给调用方用于订阅 Completed / 取 Result，不要自己再调 Release —— 模块 Dispose 时统一释放。
        /// </summary>
        public AsyncOperationHandle<T> LoadAsset<T>(string key) where T : UnityEngine.Object
        {
            ThrowIfDisposed();
            var handle = FrameworkMgr.ResMgr.LoadAssetAsync<T>(key);
            _handles.Add(handle);
            return handle;
        }

        /// <summary>
        /// 实例化一个对象并登记到组里：模块销毁时，这个实例会跟着一起被销毁、句柄一起被释放。
        /// 适合"模块活着的时候这批实例就该一直存在"的场景；如果实例的生死比模块更细粒度
        /// （比如随时会被单独回收），用 <see cref="AddressableInstantiateHelper"/> 而不是这里。
        /// </summary>
        public AsyncOperationHandle<GameObject> Instantiate(
            string key, Transform parent = null, bool instantiateInWorldSpace = false)
        {
            ThrowIfDisposed();
            var handle = FrameworkMgr.ResMgr.InstantiateAsync(key, parent, instantiateInWorldSpace);
            _handles.Add(handle);
            return handle;
        }

        /// <summary>把模块外部已经拿到的句柄也登记进来统一管理（比如从别的方法传进来的 handle）。</summary>
        public void Track(AsyncOperationHandle handle)
        {
            ThrowIfDisposed();
            if (handle.IsValid())
                _handles.Add(handle);
        }

        /// <summary>
        /// 模块销毁：把登记过的所有句柄释放一遍。可以安全地重复调用——第二次开始直接返回，不会二次释放。
        /// 单个句柄释放失败不会中断其它句柄的释放（比如某个句柄因为别的 bug 已经被提前释放过）。
        /// </summary>
        public void ReleaseAll()
        {
            if (_disposed)
                return;

            _disposed = true;

            int released = 0;
            for (int i = 0; i < _handles.Count; i++)
            {
                var handle = _handles[i];
                if (!handle.IsValid())
                    continue;

                try
                {
                    FrameworkMgr.ResMgr.Release(handle);
                    released++;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AddressableHandleGroup:{Name}] 释放句柄失败: {handle.DebugName}\n{ex}");
                }
            }

            if (_handles.Count > 0)
                Debug.Log($"[AddressableHandleGroup:{Name}] 已释放 {released}/{_handles.Count} 个句柄。");

            _handles.Clear();
        }

        /// <summary>等价于 ReleaseAll()，用于支持 using(var group = new AddressableHandleGroup(...)) 写法。</summary>
        public void Dispose() => ReleaseAll();

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AddressableHandleGroup),
                    $"模块 {Name} 的句柄组已经 ReleaseAll 过，不能再登记新句柄。");
        }
    }
}
