using System;
using TMPro;
using static GameMain.UI.UIStateCtrlFontSize;

namespace GameMain.UI
{
    [Serializable]
    public sealed class UIStateCtrlFontSize : UIStateCtrlBase<FontSizeProperty>
    {
        [Serializable]
        public class FontSizeProperty
        {
            public float fontSize;
            public float minSize;
            public float maxSize;
        }

        protected override FontSizeProperty TargetValue
        {
            set
            {
                var text = GetOrAddComponent<TextMeshProUGUI>();
                if (text.enableAutoSizing)
                {
                    text.fontSizeMin = value.minSize;
                    text.fontSizeMax = value.maxSize;
                }
                else
                {
                    text.fontSize = value.fontSize;
                }
            }

            get
            {
                var text = GetOrAddComponent<TextMeshProUGUI>();
                return new FontSizeProperty()
                {
                    fontSize = text.fontSize,
                    minSize = text.fontSizeMin,
                    maxSize = text.fontSizeMax
                };
            }
        }
    }
}
