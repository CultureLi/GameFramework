using System;
using System.Collections.Generic;

namespace Framework
{
    /// <summary>
    /// 游戏框架入口。
    /// </summary>
    public static class FrameworkMgr
    {
        private static readonly LinkedList<IFramework> _gameFrameworkModules = new LinkedList<IFramework>();

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
        public static void Shutdown()
        {
            for (LinkedListNode<IFramework> current = _gameFrameworkModules.Last; current != null; current = current.Previous)
            {
                current.Value.Shutdown();
            }

            _gameFrameworkModules.Clear();
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
                throw new Exception(Utility.Text.Format("You must get module by interface, but '{0}' is not.", interfaceType.FullName));
            }

            if (!interfaceType.FullName.StartsWith("Framework.", StringComparison.Ordinal))
            {
                throw new Exception(Utility.Text.Format("You must get a Game Framework module, but '{0}' is not.", interfaceType.FullName));
            }

            string moduleName = Utility.Text.Format("{0}.{1}", interfaceType.Namespace, interfaceType.Name.Substring(1));
            Type moduleType = Type.GetType(moduleName);
            if (moduleType == null)
            {
                throw new Exception(Utility.Text.Format("Can not find Game Framework module type '{0}'.", moduleName));
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
                throw new Exception(Utility.Text.Format("Can not create module '{0}'.", moduleType.FullName));
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
