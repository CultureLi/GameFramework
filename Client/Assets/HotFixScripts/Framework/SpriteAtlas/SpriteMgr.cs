using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Framework
{
    internal sealed partial class SpriteMgr : IFramework, ISpriteMgr
    {
        SpriteMapper _data;

        // Sprite 来源没有做地址级缓存，同一个 Sprite 可能被多处 Load，
        // 这里按引用计数释放，避免 A 释放后 B 还在用的图被卸载。
        private readonly Dictionary<Sprite, int> _spriteRefCounts = new Dictionary<Sprite, int>();

        public void Init()
        {
            AutoReleaseSprite.Init(this);

            var op = FrameworkMgr.ResMgr.LoadAsset<SpriteMapper>("Assets/AssetBundles/GlobalData/SpriteMapper.asset");
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                _data = op.Result;
            }
            else
            {
                Debug.LogError("Load SpriteMapper.asset failed!");
            }
        }

        public Sprite LoadSprite(string spriteName)
        {
            if (_data == null)
            {
                Debug.LogError("SpriteMapper is null!");
                return null;
            }

            var address = _data.GetSpriteAddress(spriteName);
            if (string.IsNullOrEmpty(address))
            {
                return null;
            }
            var op = FrameworkMgr.ResMgr.LoadAsset<Sprite>(address);
            if (op.Status == AsyncOperationStatus.Succeeded && op.Result != null)
            {
                _spriteRefCounts.TryGetValue(op.Result, out var count);
                _spriteRefCounts[op.Result] = count + 1;
                return op.Result;
            }
            Debug.LogError($"SpriteMgr:GetSprite {spriteName} failed!");
            return null;
        }

        public void ReleaseSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            if (_spriteRefCounts.TryGetValue(sprite, out var count))
            {
                if (count > 1)
                {
                    _spriteRefCounts[sprite] = count - 1;
                    return;
                }

                _spriteRefCounts.Remove(sprite);
            }
            else
            {
                Debug.LogWarning($"SpriteMgr.ReleaseSprite: '{sprite.name}' 没有对应的 Load 记录，仍然按一次引用释放。");
            }

            FrameworkMgr.ResMgr.Release(sprite);
        }

        public void Shutdown(EShutdownType type)
        {
            _spriteRefCounts.Clear();

            if (_data != null)
            {
                FrameworkMgr.ResMgr.Release(_data);
                _data = null;
            }
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
        }
    }
}
