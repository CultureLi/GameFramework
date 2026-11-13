using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Framework
{
    public class ProtoTypeHelper
    {
        private static readonly Dictionary<Type, uint> _typeToIdMap = new Dictionary<Type, uint>();
        private static readonly Dictionary<uint, Type> _idToTypeMap = new Dictionary<uint, Type>();

        // 每个消息类型只创建一个样本实例，把它的 Parser 缓存下来。
        // 收包时用它反序列化，避免每条消息都 Activator.CreateInstance 一个空对象再丢掉。
        private static readonly Dictionary<uint, MessageParser> _idToParserMap = new Dictionary<uint, MessageParser>();

        /// <summary>
        /// 已经扫描过的程序集。CollectMsgType 会被调用多次（启动时 + HybridCLR 热更程序集加载后），
        /// 只处理新加载的程序集，避免每次都把全部程序集 GetTypes 一遍。
        /// </summary>
        private static readonly HashSet<Assembly> _scannedAssemblies = new HashSet<Assembly>();

        /// <summary>所有消息都实现它；用来做类型判断。</summary>
        private static readonly Type s_messageType = typeof(IMessage);

        /// <summary>定义消息的程序集必然引用它；用来做程序集预筛选。</summary>
        private static readonly string s_protobufAssemblyName = typeof(IMessage).Assembly.GetName().Name;

        public static uint GetMsgId(Type type)
        {
            return _typeToIdMap.TryGetValue(type, out uint id) ? id : 0;
        }

        public static Type GetMsgType(uint id)
        {
            return _idToTypeMap.TryGetValue(id, out var type) ? type : null;
        }

        /// <summary>
        /// 获取消息类型的反序列化器，收包线程使用。
        /// </summary>
        public static MessageParser GetMsgParser(uint id)
        {
            return _idToParserMap.TryGetValue(id, out var parser) ? parser : null;
        }

        /// <summary>
        /// BKDRHash, 通过消息名字计算出消息id, server要和client保持算法一致
        /// seed 一般取个质数 31、131、1313、13131
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        static uint BKDRHash(string s)
        {
            uint seed = 131;
            uint hash = 0;

            for (int i = 0; i < s.Length; i++)
            {
                hash = hash * seed + s[i];
            }
            return hash;
        }

        /// <summary>
        /// 收集所有消息，做 Type 和 id 的双向映射。
        /// 可以重复调用：HybridCLR 热更程序集是在启动后才 Assembly.Load 的，
        /// 所以 GameEntryMgr.Entry 里需要再调用一次，确保热更程序集里的消息也注册进来。
        ///
        /// 优化点：
        /// 1. 按程序集增量扫描 —— 已处理过的程序集直接跳过，第二次调用只处理新加载的；
        /// 2. 先用程序集引用预筛选 —— 定义消息的程序集必然引用 Google.Protobuf，
        ///    这样可以跳过 UnityEngine / System / 第三方插件等绝大多数程序集的 GetTypes；
        /// 3. 碰撞检测用字典 O(1) 查找，不再对 values 做线性 ContainsValue。
        /// </summary>
        //[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void CollectMsgType()
        {
            int scannedAssemblies = 0;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (_scannedAssemblies.Contains(assembly))
                    continue;

                if (!MayContainMessages(assembly))
                {
                    _scannedAssemblies.Add(assembly);
                    continue;
                }

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    // 某些程序集有加载失败的依赖类型，能拿到的类型继续注册
                    types = e.Types.Where(t => t != null).ToArray();
                }
                catch (Exception e)
                {
                    // 拿不到类型就先不算扫描过，下次调用还有机会重试
                    Debug.LogWarning($"[ProtoTypeHelper] 扫描程序集 {assembly.GetName().Name} 失败: {e.Message}");
                    continue;
                }

                foreach (var type in types)
                    TryRegister(type);

                _scannedAssemblies.Add(assembly);
                scannedAssemblies++;
            }

            Debug.Log($"[ProtoTypeHelper] 已注册 {_typeToIdMap.Count} 个 protobuf 消息"
                      + (scannedAssemblies > 0 ? $"（本次扫描 {scannedAssemblies} 个程序集）" : ""));
        }

        private static void TryRegister(Type type)
        {
            if (type == null || !type.IsClass || type.IsAbstract)
                return;

            if (!s_messageType.IsAssignableFrom(type))
                return;

            uint msgId = BKDRHash(type.Name);
            if (_idToTypeMap.TryGetValue(msgId, out var existing))
            {
                // 同名类型出现在两个程序集里时（例如编辑器里又 Assembly.Load 了一份热更 DLL），
                // 两边 FullName 和 hash 都一样，这里把程序集名也打出来方便定位。
                Debug.LogError($"[碰撞] 消息 {type.FullName} 的 msgId 与其他消息重复: {existing.FullName}"
                               + $"（{type.Assembly.GetName().Name} vs {existing.Assembly.GetName().Name}）");
                return;
            }

            _typeToIdMap[type] = msgId;
            _idToTypeMap[msgId] = type;

            try
            {
                var sample = (IMessage)Activator.CreateInstance(type);
                _idToParserMap[msgId] = sample.Descriptor.Parser;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ProtoTypeHelper] 无法创建消息 {type.FullName} 的解析器: {e.Message}");
            }
        }

        /// <summary>
        /// 消息类型必须实现 <see cref="IMessage"/>，因此定义它的程序集一定引用 Google.Protobuf。
        /// 用引用信息做预筛选，避免对上百个与协议无关的程序集调用 GetTypes（反射开销很大）。
        /// </summary>
        private static bool MayContainMessages(Assembly assembly)
        {
            if (assembly.IsDynamic)
                return false;

            if (assembly.GetName().Name == s_protobufAssemblyName)
                return true;

            try
            {
                foreach (var reference in assembly.GetReferencedAssemblies())
                {
                    if (reference.Name == s_protobufAssemblyName)
                        return true;
                }
            }
            catch (Exception)
            {
                // 取不到引用信息时保守按“可能包含”处理，交给 GetTypes + 类型过滤兜底。
                return true;
            }

            return false;
        }
    }
}
