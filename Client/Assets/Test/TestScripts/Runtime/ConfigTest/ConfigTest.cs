using cfg;
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
        var itemTable = FW.CfgMgr.GetTable<TbItemSummary>();
        foreach (var item in itemTable.DataList)
        {
            Debug.Log(item.ToString());
        }

        Debug.Log(itemTable.Get(1004).ToString());

        var localizeTable = FW.CfgMgr.GetTable<Tbi18n>();
        foreach (var item in localizeTable.DataList)
        {
            Debug.Log(item.ToString());
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
