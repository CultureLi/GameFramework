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

#if !UNITY_EDITOR
            var assembly = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "GameMain");
#else
            var assembly = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "GameMain");
#endif

            if (assembly == null)
            {
                Debug.LogError("没有找到GameMain");
            }
            Type entry = assembly?.GetType(" GameMain.GameMainEntry");
            if (entry == null)
            {
                Debug.LogError("没有找到GameMain.GameMainEntry 入口");
            }
            entry?.GetMethod("Entry").Invoke(null, null);
        }

        void OverrideCatalogHash()
        {
            if (GameEntryMgr.I.IsCatalogHashChanged())
                File.WriteAllText(PathDefine.persistentCatalogHashPath, GameEntryMgr.I.RemoteCatalogHash);
        }
    }
}
