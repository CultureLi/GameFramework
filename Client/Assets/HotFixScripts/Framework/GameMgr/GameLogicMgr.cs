using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Framework
{
    public abstract class GameMgrBase
    {
        public virtual void Initialize()
        {
        }
        public virtual void Dispose()
        {
        }

        /// <summary>
        /// 清理 <see cref="GameMgrBase{T}"/> 上的静态实例引用。
        /// 不清理的话，Shutdown 之后 CreateMgr&lt;T&gt; 会直接返回这个已销毁的旧实例，
        /// 导致它既不会被重新 Initialize，也不会再收到 SecondUpdate。
        /// </summary>
        internal virtual void ResetStaticInstance()
        {
        }
    }

    public abstract class GameMgrBase<T> : GameMgrBase where T : GameMgrBase<T>, new()
    {
        public static T I
        {
            get; internal set;
        }

        internal override void ResetStaticInstance()
        {
            I = null;
        }
    }

    public class GameLogicMgr : IGameLogicMgr, IFramework
    {
        private readonly List<GameMgrBase> _mgrs = new();

        public T CreateMgr<T>() where T : GameMgrBase<T>, new()
        {
            // 已存在直接返回
            if (GameMgrBase<T>.I != null)
                return GameMgrBase<T>.I;

            // 创建
            var mgr = new T();

            // 绑定 instance
            GameMgrBase<T>.I = mgr;

            // 存入容器
            _mgrs.Add(mgr);

            return mgr;
        }

        public Action OnInitialize { get; set;}
        public void Initialize()
        {
            // 用下标遍历并实时读 Count：Manager 在 Initialize 里再 CreateMgr 出来的新 Manager
            // 也会在本次初始化里被初始化到，而不是等到下一次。
            for (int i = 0; i < _mgrs.Count; i++)
            {
                _mgrs[i].Initialize();
            }
            OnInitialize.InvokeSafely();
        }

        public void Shutdown(EShutdownType type)
        {
            foreach (var mgr in _mgrs)
            {
                mgr.Dispose();
                mgr.ResetStaticInstance();
            }
            _mgrs.Clear();
        }


        private float _secondUpdateTimer = 0f;
        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            // 用框架传入的真实时间累加，而不是自己读 Time.realtimeSinceStartup：
            // 后者在一帧内多次调用、暂停、回放时行为和框架的节拍不一致。
            _secondUpdateTimer += realElapseSeconds;
            if (_secondUpdateTimer < 1f)
            {
                return;
            }

            _secondUpdateTimer -= 1f;
            for (int i = 0; i < _mgrs.Count; i++)
            {
                if (_mgrs[i] is ISecondUpdate secondUpdate)
                {
                    secondUpdate.SecondUpdate();
                }
            }
        }
    }

}