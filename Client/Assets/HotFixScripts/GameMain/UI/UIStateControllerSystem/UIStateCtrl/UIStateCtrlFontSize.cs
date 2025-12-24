using System;
using TMPro;

namespace GameMain.UI
{
    [Serializable]
    public sealed class UIStateCtrlFontSize : UIStateCtrlBase<float>
    {
        protected override float TargetValue
        {
            set
            {
                var text = GetOrAddComponent<TextMeshProUGUI>();
                text.fontSize = value;
            }

            get
            {
                var text = GetOrAddComponent<TextMeshProUGUI>();
                return text.fontSize;
            }
        }
    }
}
