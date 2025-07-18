using cfg;
using Framework;
using GameEntry;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace TestRuntime
{
    public class ConfigTest : MonoBehaviour
    {
        // Start is called before the first frame update
        void Awake()
        {
            FW.I.Initialize();
        }

        private void OnEnable()
        {

            var buildTable = FW.CfgMgr.GetTable<TbBuildingSummaryCfg>();
            Debug.Log(buildTable.Get(5).ToString());

            //lazyLoad
            var resTable = FW.CfgMgr.GetTable<TbResourceSummaryCfg>();
            Debug.Log(resTable.Get(1002).ToString());


            //FW.LocalizationMgr.Language = "tw";
            Debug.Log($"本地化 {FW.LocalizationMgr.Get("LC_UI_Open")}");

            Debug.Log($"本地化 {FW.LocalizationMgr.Format("LC_Mail_GetItem", FW.LocalizationMgr.Get("LC_Item_Food"))}");

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}