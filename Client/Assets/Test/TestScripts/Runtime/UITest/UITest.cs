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
            //GameEntry.FW.UIMgr.AddUIGroup(,normalRoot);

            
        }

        public void Open(string name)
        {
            FW.UIMgr.OpenPopup(name);
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