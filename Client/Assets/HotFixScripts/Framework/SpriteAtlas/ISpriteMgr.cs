using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Framework
{
    public interface ISpriteMgr
    {
        void Init(IResourceMgr resMgr);
        Sprite LoadSprite(string spriteName);
        void ReleaseSprite(Sprite sprite);
    }
}
