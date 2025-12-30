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

        public static List<GameObject> CloneGameObject(GameObject template, int count)
        {
            var result = new List<GameObject>();
            var parent = template.transform.parent;
            var childCnt = parent.childCount;
            for (int i = childCnt; i < count; i++)
            {
                GameObject.Instantiate<GameObject>(template, parent);
            }

            childCnt = parent.childCount;
            for (int i = 0; i < childCnt; i++)
            {
                var child = parent.GetChild(i);
                child.gameObject.SetActive(i < count);
                if (i < count)
                {
                    result.Add(child.gameObject);
                }
            }

            return result;
        }
    }
}
