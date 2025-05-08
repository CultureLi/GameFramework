using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Framework
{
    public static class TcpUtility
    {
        private static Dictionary<Type, uint> _typeToIdMap = new Dictionary<Type, uint>();
        private static Dictionary<uint, Type> _IdToTypeMap = new Dictionary<uint, Type>();

        public static uint GetMsgId(Type type)
        {
            return _typeToIdMap.TryGetValue(type, out uint id) ? id : 0;
        }

        public static Type GetMsgType(uint id)
        {
            return _IdToTypeMap.TryGetValue(id, out var type) ? type : null; 
        }

        /// <summary>
        /// Fnv1aHash算法, 通过消息类型计算出消息id, server要和client保持算法一致
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static uint TypeToId (Type type)
        {
            const uint fnvPrime = 0x01000193; //   16777619
            const uint offsetBasis = 0x811C9DC5; // 2166136261

            uint hash = offsetBasis;
            var name = type.Name;
            foreach (char c in name)
            {
                hash ^= c;
                hash *= fnvPrime;
            }

            return hash;
        }

        public static void CollectMsgTypeId()
        {
            Clear();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (!type.IsClass || type.IsAbstract)
                        continue;

                    if (!typeof(IMessage).IsAssignableFrom(type))
                        continue;

                    uint msgId = TypeToId(type);
                    if (_typeToIdMap.ContainsValue(msgId))
                    {
                        Debug.LogError($"[碰撞] 消息 {type} 的 msgId 与其他消息重复: {msgId}");
                        continue;
                    }

                    _typeToIdMap[type] = msgId;
                    _IdToTypeMap[msgId] = type;
                }
            }
        }

        public static void Clear()
        {
            _typeToIdMap.Clear();
            _IdToTypeMap.Clear();
        }

    }
}
