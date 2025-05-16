using GameEntry;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Test.Runtime.UITest
{
    public class UITest : MonoBehaviour
    {
        public Transform normalRoot;
        // Start is called before the first frame update

        private void Awake()
        {
            
        }
        void Start()
        {
            FW.UIMgr.AddUIGroup(Framework.UIGroupType.Normal,normalRoot);

            
        }

        public void Open(string name)
        {
            FW.UIMgr.OpenUI(name, Framework.UIGroupType.Normal);
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}