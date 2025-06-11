using Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;

namespace Framework
{
    public static class ButtonExtension
    {
        public static void AddSafeListener(this Button btn, Action action)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => { action.InvokeSafely(); });
        }
    }
}
