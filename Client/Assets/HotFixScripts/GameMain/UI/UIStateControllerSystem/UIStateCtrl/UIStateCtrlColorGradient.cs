using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;

namespace GameMain.UI
{
    /// <summary>
    /// 颜色渐变
    /// </summary>
    [Serializable]
    public sealed class UIStateCtrlColorGradient : UIStateCtrlBase<VertexGradient>
    {
        protected override VertexGradient TargetValue
        {
            set
            {
                var text = GetOrAddComponent<TextMeshProUGUI>();
                text.colorGradient = value;
            }

            get
            {
                var text = GetOrAddComponent<TextMeshProUGUI>();
                return text.colorGradient;
            }
        }
    }
}
