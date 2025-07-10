using cfg;
using Framework;
using GameEntry;
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

            foreach (var zipName in new string[] { "configData.zip", "i18n.zip" })
            {
                var stream = new FileStreamEx(Path.Combine(Application.streamingAssetsPath, $"Config/{zipName}"));

                ZipArchive archive = new ZipArchive(stream.Stream, ZipArchiveMode.Read);

                FW.CfgMgr.AddZipArchive(zipName, archive);
            }
            
        }

        private void OnEnable()
        {

            var buildTable = FW.CfgMgr.GetTable<TbBuildingSummary>();
            Debug.Log(buildTable.Get(5).ToString());

            //lazyLoad
            var resTable = FW.CfgMgr.GetTable<TbResourceSummary>();
            Debug.Log(resTable.Get(1002).ToString());


            FW.LocalizationMgr.Language = "tw";
            Debug.Log($"±¾µØ»¯ {FW.LocalizationMgr.Get("LC_UI_Open")}");

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}