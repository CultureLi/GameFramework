using Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Test.Runtime.UITest
{
    internal class UIRole : ViewBase
    {
        public TextMeshProUGUI TextTest;

        public override void OnShow(bool isInitShow, ViewData data)
        {
            base.OnShow(isInitShow, data);
            Debug.Log("UIRole Show");
            TextTest.text = "Hello Role";
        }

        public override void OnClose()
        {
            base.OnClose();
            Debug.Log("UIRole Close");
        }
    }
}
