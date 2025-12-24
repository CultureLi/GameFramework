using System;
using TMPro;
using UnityEngine;

namespace GameMain.UI
{
    [Serializable]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class UIStateCtrlText : UIStateCtrlBase<string>
    {
        protected override string TargetValue
        {
            set
            {
                var text = GetOrAddComponent<TextMeshProUGUI>();
                text.text = value;
            }

            get
            {
                var tmpText = GetOrAddComponent<TextMeshProUGUI>();
                return tmpText.text;
            }
        }
    }
}
