using UnityEditor;
using UnityEngine;

namespace Test.Inspector.ObjectPoolTest
{
    [CustomEditor(typeof(Runtime.ObjectPoolTest.ObjectPoolTest))]
    public class ObjectPoolTestEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // 画默认 Inspector 界面
            base.OnInspectorGUI();

            // 获取目标对象引用
            var myScript = (Runtime.ObjectPoolTest.ObjectPoolTest)target;

            // 添加按钮
            if (GUILayout.Button("Spawn"))
            {
                myScript.Spawn();
            }
            else if (GUILayout.Button("UnSpawn"))
            {
                myScript.UnSpawn();
            }
            else if (GUILayout.Button("Count"))
            {
                myScript.Count();
            }

        }
    }
}
