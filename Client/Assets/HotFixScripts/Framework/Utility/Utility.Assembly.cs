using System;
using System.Collections.Generic;

namespace Framework
{
    public static partial class Utility
    {
        /// <summary>
        /// 程序集相关的实用函数。
        /// </summary>
        public static class Assembly
        {
            private static readonly Dictionary<string, Type> _cachedTypes = new Dictionary<string, Type>(StringComparer.Ordinal);

            /// <summary>
            /// 获取已加载的程序集。
            /// 每次都向 AppDomain 取，而不是静态构造时快照：
            /// HybridCLR 的热更程序集是在启动之后才 Assembly.Load 的，快照会永远漏掉它们。
            /// </summary>
            public static System.Reflection.Assembly[] GetAssemblies()
            {
                return AppDomain.CurrentDomain.GetAssemblies();
            }

            /// <summary>
            /// 获取已加载的程序集中的所有类型。
            /// </summary>
            /// <returns>已加载的程序集中的所有类型。</returns>
            public static Type[] GetTypes()
            {
                var assemblies = GetAssemblies();
                List<Type> results = new List<Type>();
                foreach (System.Reflection.Assembly assembly in assemblies)
                {
                    results.AddRange(assembly.GetTypes());
                }

                return results.ToArray();
            }

            /// <summary>
            /// 获取已加载的程序集中的所有类型。
            /// </summary>
            /// <param name="results">已加载的程序集中的所有类型。</param>
            public static void GetTypes(List<Type> results)
            {
                if (results == null)
                {
                    throw new Exception("Results is invalid.");
                }

                results.Clear();
                foreach (System.Reflection.Assembly assembly in GetAssemblies())
                {
                    results.AddRange(assembly.GetTypes());
                }
            }

            /// <summary>
            /// 获取已加载的程序集中的指定类型。
            /// </summary>
            /// <param name="typeName">要获取的类型名。</param>
            /// <returns>已加载的程序集中的指定类型。</returns>
            public static Type GetType(string typeName)
            {
                if (string.IsNullOrEmpty(typeName))
                {
                    throw new Exception("Type name is invalid.");
                }

                Type type = null;
                if (_cachedTypes.TryGetValue(typeName, out type))
                {
                    return type;
                }

                type = Type.GetType(typeName);
                if (type != null)
                {
                    _cachedTypes.Add(typeName, type);
                    return type;
                }

                foreach (System.Reflection.Assembly assembly in GetAssemblies())
                {
                    type = Type.GetType($"{typeName}, {assembly.FullName}");
                    if (type != null)
                    {
                        _cachedTypes.Add(typeName, type);
                        return type;
                    }
                }

                return null;
            }
        }
    }
}
