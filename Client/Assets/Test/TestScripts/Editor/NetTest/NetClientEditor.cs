﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestRuntime;
using UnityEditor;
using UnityEngine;

namespace Test.Inspector.NetTest
{
    [CustomEditor(typeof(ClientNet))]
    public class NetClientEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // 画默认 Inspector 界面
            base.OnInspectorGUI();

            // 获取目标对象引用
            var myScript = (ClientNet)target;

            // 添加按钮
            if (GUILayout.Button("Send"))
            {
                myScript.Send();
            }

        }
    }
}
