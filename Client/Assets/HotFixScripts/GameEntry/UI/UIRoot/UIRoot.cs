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
        public GameObject template;
        protected void Awake()
        {
            template.SetActive(false);
            for (var type = EUIGroupType.HUD; type <= EUIGroupType.Tips; type++)
            {
                var groupRoot = GameObject.Instantiate(template, transform);
                groupRoot.SetActive(true);
                groupRoot.name = Enum.GetName(typeof(EUIGroupType), type);
                FW.UIMgr.AddGroup(type, groupRoot.transform);
            }
        }
    }
}
