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
            if (_sprite != null)
            {
                _spriteMgr.ReleaseSprite(_sprite);
            }
        }
        private void OnDestroy()
        {
            ReleaseSprite();
        }
    }
}
