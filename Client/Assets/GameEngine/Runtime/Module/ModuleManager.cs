using GameEngine.Runtime.Base;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine.Runtime.Module
{
    /// <summary>
    /// 游戏模块管理器
    /// </summary>
    public class ModuleManager:Singleton<ModuleManager>
    {
        private readonly List<ModuleBase> s_Modules = new();
        private Transform m_ModuleRoot;

        public void Init()
        {
            var go = new GameObject("ModuleRoot");
            GameObject.DontDestroyOnLoad(go);
            m_ModuleRoot = go.transform;
        }

        /// <summary>
        /// 所有模块轮询
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            foreach (var module in s_Modules)
            {
                module.OnUpdate(elapseSeconds, realElapseSeconds);
            }
        }

        /// <summary>
        /// 所有模块轮询
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        public void FixUpdate(float elapseSeconds, float realElapseSeconds)
        {
            foreach (var module in s_Modules)
            {
                module.OnFixUpdate(elapseSeconds, realElapseSeconds);
            }
        }

        /// <summary>
        /// 所有模块轮询
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        public void LateUpdate(float elapseSeconds, float realElapseSeconds)
        {
            foreach (var module in s_Modules)
            {
                module.OnLateUpdate(elapseSeconds, realElapseSeconds);
            }
        }

        /// <summary>
        /// 关闭并清理所有模块
        /// </summary>
        public void Release()
        {
            foreach (var module in s_Modules)
            {
                module.Release();
            }

            s_Modules.Clear();
            //ReferencePool.ClearAll();
            //Utility.Marshal.FreeCachedHGlobal();
            //GameFrameworkLog.SetLogHelper(null);
        }

        /// <summary>
        /// 获取模块
        /// </summary>
        public T GetModule<T>() where T : ModuleBase
        {
            return GetModule(typeof(T)) as T;
        }

        /// <summary>
        /// 获取模块
        /// </summary>
        private ModuleBase GetModule(Type moduleType)
        {
            foreach (var module in s_Modules)
            {
                if (module.GetType() == moduleType)
                {
                    return module;
                }
            }

            return CreateModule(moduleType);
        }

        /// <summary>
        /// 创建模块
        /// </summary>
        private ModuleBase CreateModule(Type moduleType)
        {
            var go = new GameObject(moduleType.Name);
            go.transform.parent = m_ModuleRoot;
            var module = go.AddComponent(moduleType) as ModuleBase;
            if (module == null)
                throw new Exception($"Create Module:{moduleType.Name} Fail !!!");

            var count = s_Modules.Count;
            var insertIdx = count;
            for (int i = 0; i < count; i++)
            {
                if (module.Priority > s_Modules[i].Priority)
                {
                    insertIdx = i;
                    break;
                }
            }

            s_Modules.Insert(insertIdx, module);

            return module;
        }
    }
}
