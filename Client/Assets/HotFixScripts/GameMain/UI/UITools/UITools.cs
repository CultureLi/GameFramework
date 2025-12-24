using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GameMain.UI
{
    public static partial class UITools
    {
        public static Color WithAlpha(this Color c, float alpha)
        {
            c.a = alpha;
            return c;
        }
    }
}
