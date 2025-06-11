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
        public Transform HUDRoot;
        public Transform NormalRoot;
        public Transform MsgBoxRoot;
        public Transform TipsRoot;


        protected void Awake()
        {
            FW.UIMgr.AddUIGroup((int)UIGroupType.HUD, HUDRoot);
            FW.UIMgr.AddUIGroup((int)UIGroupType.Normal, NormalRoot);
            FW.UIMgr.AddUIGroup((int)UIGroupType.MsgBox, MsgBoxRoot);
            FW.UIMgr.AddUIGroup((int)UIGroupType.Tips, TipsRoot);
        }
    }
}
