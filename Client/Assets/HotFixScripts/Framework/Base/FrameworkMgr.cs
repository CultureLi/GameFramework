using System;
using System.Collections.Generic;

namespace Framework
{
    /// <summary>
    /// 游戏框架入口。
    /// </summary>
    public static class FrameworkMgr
    {
        public static EShutdownType ShutdownType { get; private set; } = EShutdownType.None;
        private static readonly LinkedList<IFramework> _gameFrameworkModules = new LinkedList<IFramework>();

        // 各模块的强类型缓存：Framework 内部和 GameEntry.FW 统一从这里取，
        // 避免每次都在 GetModule 里做接口名拼接 + Type.GetType + 链表扫描。
        // 注意：不能在模块构造函数里访问这些属性——CreateModule 是"先构造、后注册"，会造成重入创建。
        private static IEventMgr s_eventMgr;
        private static IFsmMgr s_fsmMgr;
        private static IResourceMgr s_resourceMgr;
        private static ITimerMgr s_timerMgr;
        private static INetworkMgr s_networkMgr;
        private static IObjectPoolMgr s_objectPoolMgr;
        private static IUIManager s_uiMgr;
        private static IConfigMgr s_configMgr;
        private static ILocalizationMgr s_localizationMgr;
        private static ISpriteMgr s_spriteMgr;
        private static IGameLogicMgr s_gameLogicMgr;

        public static IEventMgr EventMgr => s_eventMgr ??= GetModule<IEventMgr>();
        public static IFsmMgr FsmMgr => s_fsmMgr ??= GetModule<IFsmMgr>();
        public static IResourceMgr ResMgr => s_resourceMgr ??= GetModule<IResourceMgr>();
        public static ITimerMgr TimerMgr => s_timerMgr ??= GetModule<ITimerMgr>();
        public static INetworkMgr NetMgr => s_networkMgr ??= GetModule<INetworkMgr>();
        public static IObjectPoolMgr ObjectPoolMgr => s_objectPoolMgr ??= GetModule<IObjectPoolMgr>();
        public static IUIManager UIMgr => s_uiMgr ??= GetModule<IUIManager>();
        public static IConfigMgr CfgMgr => s_configMgr ??= GetModule<IConfigMgr>();
        public static ILocalizationMgr LocalizationMgr => s_localizationMgr ??= GetModule<ILocalizationMgr>();
        public static ISpriteMgr SpriteMgr => s_spriteMgr ??= GetModule<ISpriteMgr>();
        public static IGameLogicMgr GameLogicMgr => s_gameLogicMgr ??= GetModule<IGameLogicMgr>();

        /// <summary>
        /// 所有游戏框架模块轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        public static void Update(float elapseSeconds, float realElapseSeconds)
        {
            foreach (IFramework module in _gameFrameworkModules)
            {
                module.Update(elapseSeconds, realElapseSeconds);
            }
        }

        /// <summary>
        /// 关闭并清理所有游戏框架模块。
        /// </summary>
        public static void Shutdown(EShutdownType type)
        {
            ShutdownType = type;

            for (LinkedListNode<IFramework> current = _gameFrameworkModules.Last; current != null; current = current.Previous)
            {
                current.Value.Shutdown(type);
            }
            ReferencePool.ClearAll();
            if (type == EShutdownType.Shutdown)
            {
                _gameFrameworkModules.Clear();
            }

            // 缓存必须跟着失效：Shutdown 后模块链表可能已被清空，
            // 不清缓存的话 FrameworkMgr.ResMgr / FW.ResMgr 会拿到已销毁的实例。
            ResetModuleCache();
        }

        /// <summary>清空模块缓存。模块本身由 _gameFrameworkModules 管理，这里只处理强类型引用。</summary>
        private static void ResetModuleCache()
        {
            s_eventMgr = null;
            s_fsmMgr = null;
            s_resourceMgr = null;
            s_timerMgr = null;
            s_networkMgr = null;
            s_objectPoolMgr = null;
            s_uiMgr = null;
            s_configMgr = null;
            s_localizationMgr = null;
            s_spriteMgr = null;
            s_gameLogicMgr = null;
        }

        /// <summary>
        /// 获取游戏框架模块。
        /// </summary>
        /// <typeparam name="T">要获取的游戏框架模块类型。</typeparam>
        /// <returns>要获取的游戏框架模块。</returns>
        /// <remarks>如果要获取的游戏框架模块不存在，则自动创建该游戏框架模块。</remarks>
        public static T GetModule<T>() where T : class
        {
            Type interfaceType = typeof(T);
            if (!interfaceType.IsInterface)
            {
                throw new Exception($"You must get module by interface, but '{interfaceType.FullName}' is not.");
            }

            if (!interfaceType.FullName.StartsWith("Framework.", StringComparison.Ordinal))
            {
                throw new Exception($"You must get module by interface, but '{interfaceType.FullName}' is not.");
            }

            string moduleName = $"{interfaceType.Namespace}.{interfaceType.Name.Substring(1)}";
            Type moduleType = Type.GetType(moduleName);
            if (moduleType == null)
            {
                throw new Exception($"Can not find Game Framework module type '{moduleName}'.");
            }

            return GetModule(moduleType) as T;
        }

        /// <summary>
        /// 获取游戏框架模块。
        /// </summary>
        /// <param name="moduleType">要获取的游戏框架模块类型。</param>
        /// <returns>要获取的游戏框架模块。</returns>
        /// <remarks>如果要获取的游戏框架模块不存在，则自动创建该游戏框架模块。</remarks>
        private static IFramework GetModule(Type moduleType)
        {
            foreach (IFramework module in _gameFrameworkModules)
            {
                if (module.GetType() == moduleType)
                {
                    return module;
                }
            }

            return CreateModule(moduleType);
        }

        /// <summary>
        /// 创建游戏框架模块。
        /// </summary>
        /// <param name="moduleType">要创建的游戏框架模块类型。</param>
        /// <returns>要创建的游戏框架模块。</returns>
        private static IFramework CreateModule(Type moduleType)
        {
            IFramework module = (IFramework)Activator.CreateInstance(moduleType);
            if (module == null)
            {
                throw new Exception($"Can not create module '{moduleType.FullName}'.");
            }

            LinkedListNode<IFramework> current = _gameFrameworkModules.First;
            while (current != null)
            {
                if (module.Priority > current.Value.Priority)
                {
                    break;
                }

                current = current.Next;
            }

            if (current != null)
            {
                _gameFrameworkModules.AddBefore(current, module);
            }
            else
            {
                _gameFrameworkModules.AddLast(module);
            }

            return module;
        }
    }
}
