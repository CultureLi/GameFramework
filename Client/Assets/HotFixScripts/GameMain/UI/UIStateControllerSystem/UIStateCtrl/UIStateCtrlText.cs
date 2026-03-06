using System;
using TMPro;
using UnityEngine;

namespace GameMain.UI
{
    [Serializable]
    public sealed class UIStateCtrlText : UIStateCtrlBase<string>
    {
        protected override string TargetValue
        {
            set
            {
                var tmpText = GetOrAddComponent<GameEntry.UILocalizeText>();
                if (Application.isPlaying)
                {
                    tmpText.Key = value;
                }
                else
                {
                    var text = GetOrAddComponent<TextMeshProUGUI>();
                    text.text = value;
                }
            }

            get
            {
                var tmpText = GetOrAddComponent<GameEntry.UILocalizeText>();
                return tmpText.Key;
            }
        }
    }
}
