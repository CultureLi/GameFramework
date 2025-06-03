using GameEntry;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Test.Runtime.UITest
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
            FW.UIMgr.AddUIGroup((int)UIGroupType.Normal,normalRoot);

            
        }

        public void Open(string name)
        {
            FW.UIMgr.OpenUI(name, (int)UIGroupType.Normal);
        }

        public void Close(string name)
        {
            FW.UIMgr.CloseUI(name);
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}