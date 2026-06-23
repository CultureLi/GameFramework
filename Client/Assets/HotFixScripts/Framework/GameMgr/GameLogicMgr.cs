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
    }

    public abstract class GameMgrBase<T> : GameMgrBase where T : GameMgrBase<T>, new()
    {
        public static T I
        {
            get; internal set;
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
            foreach (var mgr in _mgrs)
            {
                mgr.Initialize();
            }
            OnInitialize.InvokeSafely();
        }

        public void Shutdown(EShutdownType type)
        {
            foreach (var mgr in _mgrs)
            {
                mgr.Dispose();
            }
            _mgrs.Clear();
        }


        float _lastSecondUpdateTime = 0;
        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            if (Time.realtimeSinceStartup - _lastSecondUpdateTime >= 1)
            {
                _lastSecondUpdateTime = Time.realtimeSinceStartup;
                foreach (var mgr in _mgrs)
                {
                    if (mgr is ISecondUpdate secondUpdate)
                    {
                        secondUpdate.SecondUpdate();
                    }
                }
            }
        }
    }

}