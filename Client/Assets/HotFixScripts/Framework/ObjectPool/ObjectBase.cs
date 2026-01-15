using System;

namespace Framework
{
    /// <summary>
    /// 对象基类。
    /// </summary>
    public abstract class ObjectBase : IReference
    {
        public string Name
        {
            get; private set;
        }

        public UnityEngine.Object Target
        {
            get; private set;
        }

        public DateTime LastUseTime
        {
            get; internal set;
        }

        /// <summary>
        /// 初始化对象基类的新实例。
        /// </summary>
        public ObjectBase()
        {
            Name = null;
            Target = null;
            LastUseTime = default(DateTime);
        }

        /// <summary>
        /// 初始化对象基类。
        /// </summary>
        /// <param name="target">对象。</param>
        protected void Initialize(UnityEngine.Object target)
        {
            Initialize(null, target);
        }

        /// <summary>
        /// 初始化对象基类。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <param name="target">对象。</param>
        protected void Initialize(string name, UnityEngine.Object target)
        {
            if (target == null)
            {
                throw new Exception($"Target '{name}' is invalid.");
            }

            Name = name ?? string.Empty;
            Target = target;
            LastUseTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 清理对象基类。
        /// </summary>
        public virtual void Clear()
        {
            Name = null;
            Target = null;
            LastUseTime = default(DateTime);
        }

        /// <summary>
        /// 获取对象时的事件。
        /// </summary>
        protected internal virtual void OnSpawn()
        {
        }

        /// <summary>
        /// 回收对象时的事件。
        /// </summary>
        protected internal virtual void OnUnspawn()
        {
        }

        /// <summary>
        /// 释放对象。
        /// </summary>
        protected internal abstract void Release();
    }
}
