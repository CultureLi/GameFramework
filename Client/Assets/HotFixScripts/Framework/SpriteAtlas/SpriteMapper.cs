using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    [CreateAssetMenu(fileName = "SpriteMapper", menuName = "ScriptableObjects/SpriteMapper")]
    public class SpriteMapper : SerializedScriptableObject
    {
        [OdinSerialize, ShowInInspector, Searchable]
        Dictionary<string, string> _map = new Dictionary<string, string>();

        public string GetSpriteAddress(string spriteName)
        {
            if (_map.TryGetValue(spriteName, out var address))
            {
                return address;
            }
            Debug.LogError($"Can't Find {spriteName} in SpriteMapper");
            return string.Empty;
        }
#if UNITY_EDITOR

        public void Add(string spriteName, string value)
        {
            if (!_map.ContainsKey(spriteName))
            {
                _map.Add(spriteName, value);
            }
            else
            {
                Debug.LogError($"存在相同命名的Sprite {spriteName}, 请重新命名");
            }
        }

        public void Clear()
        {
            _map.Clear();
        }   
#endif
    }
}
