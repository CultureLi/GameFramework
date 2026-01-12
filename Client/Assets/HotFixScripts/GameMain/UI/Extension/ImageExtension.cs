using Framework;
using GameEntry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace GameMain.UI
{
    internal static  class ImageExtension
    {
        static public void SetAlpha(this Image image, float alpha)
        {
            if (image == null)
                return;

            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }

        static public void SetSprite(this Image image, string spriteName)
        {
            var autoRelease = image.gameObject.GetOrAddComponent<AutoReleaseSprite>();
            var sprite = FW.SpriteMgr.LoadSprite(spriteName);
            autoRelease.SetSprite(sprite);
            if (sprite != null)
            {
                image.overrideSprite = sprite;
            }
        }
    }
}
