
using System;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 对象池接口。
    /// </summary>
    /// <typeparam name="T">对象类型。</typeparam>
    public interface IObjectPool<T> where T : ObjectBase
    {
        /// <summary>
        /// 获取对象池名称。
        /// </summary>
        string Name
        {
            get;
        }

        /// <summary>
        /// 获取对象池完整名称。
        /// </summary>
        string FullName
        {
            get;
        }

        /// <summary>
        /// 获取对象池对象类型。
        /// </summary>
        Type ObjectType
        {
            get;
        }

        /// <summary>
        /// 获取对象池中对象的数量。
        /// </summary>
        int Count
        {
            get;
        }

        /// <summary>
        /// 获取对象池中能被释放的对象的数量。
        /// </summary>
        int CanReleaseCount
        {
            get;
        }

        /// <summary>
        /// 获取是否允许对象被多次获取。
        /// </summary>
        bool AllowMultiSpawn
        {
            get;
        }

        /// <summary>
        /// 获取或设置对象池自动释放可释放对象的间隔秒数。
        /// </summary>
        float AutoReleaseInterval
        {
            get;
            set;
        }

        /// <summary>
        /// 获取或设置对象池的容量。
        /// </summary>
        int Capacity
        {
            get;
            set;
        }

        /// <summary>
        /// 获取或设置对象池对象过期秒数。
        /// </summary>
        float ExpireTime
        {
            get;
            set;
        }

        /// <summary>
        /// 创建对象。
        /// </summary>
        /// <param name="obj">对象。</param>
        /// <param name="spawned">对象是否已被获取。</param>
        void Register(T obj, bool spawned);

        /// <summary>
        /// 检查对象。
        /// </summary>
        /// <returns>要检查的对象是否存在。</returns>
        bool CanSpawn();

        /// <summary>
        /// 检查对象。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <returns>要检查的对象是否存在。</returns>
        bool CanSpawn(string name);

        /// <summary>
        /// 获取对象。
        /// </summary>
        /// <returns>要获取的对象。</returns>
        T Spawn();

        /// <summary>
        /// 获取对象。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <returns>要获取的对象。</returns>
        T Spawn(string name);

        /// <summary>
        /// 回收对象。
        /// </summary>
        /// <param name="obj">要回收的对象。</param>
        void Unspawn(T obj);

        /// <summary>
        /// 回收对象。
        /// </summary>
        /// <param name="target">要回收的对象。</param>
        void Unspawn(UnityEngine.Object target);

        /// <summary>
        /// 释放对象。
        /// </summary>
        /// <param name="obj">要释放的对象。</param>
        /// <returns>释放对象是否成功。</returns>
        bool ReleaseObject(T obj);

        /// <summary>
        /// 释放对象。
        /// </summary>
        /// <param name="target">要释放的对象。</param>
        /// <returns>释放对象是否成功。</returns>
        bool ReleaseObject(UnityEngine.Object target);

        /// <summary>
        /// 强制移除对象（不检查 UnityEngine.Object 是否已被销毁）。
        /// 用于"对象已被 Unity 销毁、但池子里还留着它的记录"的场景：
        /// <see cref="Unspawn(UnityEngine.Object)"/>/<see cref="ReleaseObject(UnityEngine.Object)"/>
        /// 开头都对 target 做了假 null 检查并抛异常，无法处理这种情况。
        /// 移除时会调用对象自己的 Release()，由对象决定是否销毁 GameObject / 释放句柄。
        /// </summary>
        /// <param name="target">要移除的对象（允许是已销毁的对象）。</param>
        /// <returns>是否在池中找到了该记录并移除。</returns>
        bool ForceRemoveObject(UnityEngine.Object target);

        /// <summary>
        /// 释放对象池中的可释放对象。
        /// </summary>
        void Release();

        /// <summary>
        /// 释放对象池中的可释放对象。
        /// </summary>
        /// <param name="toReleaseCount">尝试释放对象数量。</param>
        void Release(int toReleaseCount);

        /// <summary>
        /// 释放对象池中的可释放对象。
        /// </summary>
        /// <param name="releaseObjectFilterCallback">释放对象筛选函数。</param>
        void Release(ReleaseObjectFilterCallback<T> releaseObjectFilterCallback);

        /// <summary>
        /// 释放对象池中的可释放对象。
        /// </summary>
        /// <param name="toReleaseCount">尝试释放对象数量。</param>
        /// <param name="releaseObjectFilterCallback">释放对象筛选函数。</param>
        void Release(int toReleaseCount, ReleaseObjectFilterCallback<T> releaseObjectFilterCallback);

        /// <summary>
        /// 释放对象池中的所有未使用对象。
        /// </summary>
        void ReleaseAllUnused();

        /// <summary>
        /// 设置对象是否允许被自动释放（UI 模块用它控制回收时机）。
        /// </summary>
        void SetCanBeReleased(GameObject go, bool canBeReleased);
    }

    public interface IInstanceOwner
    {
        /// <summary>
        /// 实例被销毁时的回调。
        /// 用 int 而不是 GameObject：回调发生在 OnDestroy 期间，此时 GameObject 已经是
        /// Unity 的"假 null"，按引用传递没有意义；GetInstanceID 的结果在销毁后依然稳定。
        /// </summary>
        /// <param name="instanceId">被销毁实例的 instanceId。</param>
        void OnInstanceDestroyed(int instanceId);
    }
}
