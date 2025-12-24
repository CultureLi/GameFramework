using Framework;
using GameEntry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GameEntry
{
    internal class UIRoot : MonoBehaviour
    {
        protected void Awake()
        {
            for (var i = UIGroupType.HUD; i <= UIGroupType.Tips; i++)
            {
                var name = Enum.GetName(typeof(UIGroupType), i);
                FW.UIMgr.AddUIGroup((int)i, transform.Find(name));
            }
        }
    }
}
