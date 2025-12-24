using System;
using UnityEngine;

namespace GameMain.UI
{
    [Serializable]
    public sealed class UIStateCtrlSize : UIStateCtrlBase<Vector2>
    {
        protected override Vector2 TargetValue
        {
            set
            {
                var rt = transform as RectTransform;
                rt.sizeDelta = value;
            }

            get
            {
                var rt = transform as RectTransform;
                return rt.sizeDelta;
            }
        }
    }
}
