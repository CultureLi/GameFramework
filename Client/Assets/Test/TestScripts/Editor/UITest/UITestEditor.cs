using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

namespace Test.Inspector.UITest
{
    [CustomEditor(typeof(Runtime.UITest.UITest))]
    internal class UITestEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // 画默认 Inspector 界面
            base.OnInspectorGUI();

            // 获取目标对象引用
            var myScript = (Runtime.UITest.UITest)target;

            // 添加按钮
            if (GUILayout.Button("OpenMail"))
            {
                myScript.Open("Mail/UIMail");
            }
            else if (GUILayout.Button("CloseMail"))
            {
                myScript.Close("Mail/UIMail");
            }
            else if (GUILayout.Button("OpenRole"))
            {
                myScript.Open("Mail/UIRole");
            }
            else if (GUILayout.Button("CloseRole"))
            {
                myScript.Close("Mail/UIRole");
            }

        }
    }
}
