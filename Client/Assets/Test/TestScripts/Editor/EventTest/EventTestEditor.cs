using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

namespace Test.Inspector.EventTest
{
    [CustomEditor(typeof(Runtime.EventTest.EventTest))]
    internal class EventTestEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // 画默认 Inspector 界面
            base.OnInspectorGUI();

            // 获取目标对象引用
            var myScript = (Runtime.EventTest.EventTest)target;

            // 添加按钮
            if (GUILayout.Button("SubscribeHpChangedHandler1"))
            {
                myScript.SubscribeHpChangedHandler1();
            }
            else if (GUILayout.Button("SubscribeHpChangedHandler2"))
            {
                myScript.SubscribeHpChangedHandler2();
            }
            else if (GUILayout.Button("SubscribeCustomEventHandler"))
            {
                myScript.SubscribeCustomEventHandler();
            }
            else if (GUILayout.Button("UnSubscribeHpChangedHandler1"))
            {
                myScript.UnSubscribeHpChangedHandler1();
            }
            else if (GUILayout.Button("UnSubscribeHpChangedHandler2"))
            {
                myScript.UnSubscribeHpChangedHandler2();
            }
            else if (GUILayout.Button("UnSubscribeCustomEventHandler"))
            {
                myScript.UnSubscribeCustomEventHandler();
            }
            else if (GUILayout.Button("广播事件"))
            {
                myScript.BroadCastEvent();
            }

        }
    }
}
