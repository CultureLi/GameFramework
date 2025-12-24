using UnityEngine;
using UnityEngine.UI;

namespace GameMain.UI
{
    [SerializeField]
    public sealed class UIStateCtrlImage : UIStateCtrlBase<Sprite>
    {
        protected override Sprite TargetValue
        {
            set
            {
                var img = GetOrAddComponent<Image>();
                img.sprite = value;
            }

            get
            {
                var img = GetOrAddComponent<Image>();
                return img.sprite;
            }
        }
    }
}
