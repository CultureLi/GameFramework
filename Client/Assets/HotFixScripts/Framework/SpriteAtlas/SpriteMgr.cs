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
        IResourceMgr _resMgr;
        public void Init(IResourceMgr resMgr)
        {
            AutoReleaseSprite.Init(this);

            _resMgr = resMgr;
            var op = resMgr.LoadAsset<SpriteMapper>("Assets/BundleRes/ScriptableObject/SpriteMapper.asset");
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
            var op = _resMgr.LoadAsset<Sprite>(address);
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
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
            _resMgr.Release(sprite);
        }

        public void Shutdown()
        {
            _resMgr.Release(_data);
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
        }
    }
}
