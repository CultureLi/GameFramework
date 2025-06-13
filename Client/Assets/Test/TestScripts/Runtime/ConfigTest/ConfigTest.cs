using cfg;
using cfg.Item;
using GameEntry;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConfigTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        FW.I.Initialize();
       
    }

    private void OnEnable()
    {
        var cfgList = FW.CfgMgr.GetTable<TbItemSummary>();
        foreach (var item in cfgList.DataList)
        {
            Debug.Log(item.ToString());
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
