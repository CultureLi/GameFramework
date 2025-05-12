using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
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
        /// 初始化，收集所有消息，做Type和id的双向映射
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void CollectMsgType()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
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
        }
    }
}
