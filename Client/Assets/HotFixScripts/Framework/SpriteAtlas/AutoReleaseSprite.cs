using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Framework
{
    public class AutoReleaseSprite : MonoBehaviour
    {
        static ISpriteMgr _spriteMgr;
        public static void Init(ISpriteMgr spriteMgr)
        {
            _spriteMgr = spriteMgr;
        }
        Sprite _sprite;

        public void SetSprite(Sprite sprite)
        {
            ReleaseSprite();
            _sprite = sprite;
        }

        public void ReleaseSprite()
        {
            if (_sprite == null)
            {
                return;
            }

            var sprite = _sprite;
            // 先清引用再释放：否则 OnDestroy 会对同一个 Sprite 再释放一次，
            // Addressables 的引用计数会被减到 0 以下。
            _sprite = null;

            if (_spriteMgr != null)
            {
                _spriteMgr.ReleaseSprite(sprite);
            }
        }
        private void OnDestroy()
        {
            ReleaseSprite();
        }
    }
}
