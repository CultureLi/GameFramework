using GameEntry;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TestRuntime
{
    public enum UIGroupType
    {
        Normal = 1,
    }
    public class UITest : MonoBehaviour
    {
        public Transform normalRoot;
        // Start is called before the first frame update

        private void Awake()
        {
            
        }
        void Start()
        {
            GameEntry.FW.UIMgr.AddUIGroup((int)UIGroupType.Normal, normalRoot);

            
        }

        public void Open(string name)
        {
            GameEntry.FW.UIMgr.OpenUI(name, (int)UIGroupType.Normal);
        }

        public void Close(string name)
        {
            GameEntry.FW.UIMgr.CloseUI(name);
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}