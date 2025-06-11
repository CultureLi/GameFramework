using AOTBase;
using Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GameEntry.Stage
{
    internal class EntranceEndStage : FsmState
    {
        protected override void OnEnter()
        {
            OverrideCatalogHash();

            FW.UIMgr.OpenUI("GameEntry/UIGameEntryLogin", (int)UIGroupType.Normal);
        }

        void OverrideCatalogHash()
        {
            if (GameEntryMgr.I.IsCatalogHashChanged())
                File.WriteAllText(PathDefine.persistentCatalogHashPath, GameEntryMgr.I.RemoteCatalogHash);
        }
    }
}
