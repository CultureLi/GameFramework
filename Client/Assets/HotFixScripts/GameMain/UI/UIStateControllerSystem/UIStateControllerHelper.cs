#if UNITY_EDITOR
using GameMain.UI;
using UnityEngine;

namespace GameMain.UI
{
    public static class UIStateControllerHelper
    {
        public static Color stateHighLightColor = Color.green;
        public static Color listLightColor = stateHighLightColor.WithAlpha(0.3f);
    }
}
#endif