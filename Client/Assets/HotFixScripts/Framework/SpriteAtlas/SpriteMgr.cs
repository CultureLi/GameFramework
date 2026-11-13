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
            FrameworkMgr.ResMgr.Release(sprite);
        }

        public void Shutdown(EShutdownType type)
        {
            FrameworkMgr.ResMgr.Release(_data);
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
        }
    }
}
