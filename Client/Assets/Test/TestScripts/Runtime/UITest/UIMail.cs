using Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TestRuntime
{
    internal class UIMail : ViewBase
    {
        public TextMeshProUGUI TextTest;

        public override void OnShow(bool isInitShow, ViewData data)
        {
            base.OnShow(isInitShow, data);
            Debug.Log("UIMail Show");
            TextTest.text = "Hello world";
        }

        public override void OnClose()
        {
            base.OnClose();
            Debug.Log("UIMail Close");
        }
    }
}
