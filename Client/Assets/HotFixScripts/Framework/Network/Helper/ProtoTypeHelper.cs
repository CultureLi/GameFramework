using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Framework
{
    public class ProtoTypeHelper
    {
        private static Dictionary<Type, uint> _typeToIdMap = new Dictionary<Type, uint>();
        private static Dictionary<uint, Type> _idToTypeMap = new Dictionary<uint, Type>();

        public static uint GetMsgId(Type type)
        {
            return _typeToIdMap.TryGetValue(type, out uint id) ? id : 0;
        }

        public static Type GetMsgType(uint id)
        {
            return _idToTypeMap.TryGetValue(id, out var type) ? type : null; 
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
        /// 初始化，收集所有消息，做Type和id的双向映射。
        /// 可以重复调用：HybridCLR 热更程序集是在启动后才 Assembly.Load 的，
        /// 所以 GameEntryMgr.Entry 里需要再调用一次，确保热更程序集里的消息也注册进来。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void CollectMsgType()
        {
            _typeToIdMap.Clear();
            _idToTypeMap.Clear();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
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
                    Debug.LogWarning($"[ProtoTypeHelper] 扫描程序集 {assembly.GetName().Name} 失败: {e.Message}");
                    continue;
                }

                foreach (var type in types)
                {
                    if (type == null)
                        continue;

                    if (!type.IsClass || type.IsAbstract)
                        continue;

                    if (!typeof(IMessage).IsAssignableFrom(type))
                        continue;

                    uint msgId = BKDRHash(type.Name);
                    if (_typeToIdMap.ContainsValue(msgId))
                    {
                        Debug.LogError($"[碰撞] 消息 {type} 的 msgId 与其他消息重复: {msgId}");
                        continue;
                    }

                    _typeToIdMap[type] = msgId;
                    _idToTypeMap[msgId] = type;
                }
            }

            Debug.Log($"[ProtoTypeHelper] 已注册 {_typeToIdMap.Count} 个 protobuf 消息");
        }
    }
}
