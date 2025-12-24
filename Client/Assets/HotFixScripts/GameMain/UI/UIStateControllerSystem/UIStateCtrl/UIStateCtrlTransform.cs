using System;
using UnityEngine;
using static GameMain.UI.UIStateCtrlTransform;

namespace GameMain.UI
{
    [Serializable]
    public sealed class UIStateCtrlTransform : UIStateCtrlBase<TransformProperty>
    {
        [Serializable]
        public class TransformProperty
        {
            public Vector3 position;
            public Vector3 rotation;
            public Vector3 scale;
        }
        protected override TransformProperty TargetValue
        {
            set
            {
                var rt = transform as RectTransform;
                rt.localPosition = value.position;
                rt.localRotation = Quaternion.Euler(value.rotation);
                rt.localScale = value.scale;
            }

            get
            {
                var rt = transform as RectTransform;
                return new TransformProperty()
                {
                    position = rt.localPosition,
                    rotation = rt.localRotation.eulerAngles,
                    scale = rt.localScale
                };
            }
        }
    }
}
