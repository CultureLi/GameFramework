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

            if (GameEntryMgr.I.NeedRestart)
            {
                var uiData = new GameEntryMsgBoxData()
                {
                    content = $"Need Restart App",
                    callback = (result) =>
                    {
                        if (result)
                        {
                            AppHelper.RestartApp();
                        }
                        else
                        {
                            AppHelper.QuitGame();
                        }
                    }
                };
                FW.UIMgr.OpenPopup("GameEntry/UIGameEntryMsgBox", uiData);
            }
            else
            {
                FW.UIMgr.OpenPopup("GameEntry/UIGameEntryLogin");
            }
        }

        void OverrideCatalogHash()
        {
            if (GameEntryMgr.I.IsCatalogHashChanged())
                File.WriteAllText(PathDefine.persistentCatalogHashPath, GameEntryMgr.I.RemoteCatalogHash);
        }
    }
}
