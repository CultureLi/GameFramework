using TestRuntime;
using UnityEditor;
using UnityEngine;

namespace TestEditor
{
    [CustomEditor(typeof(ObjectPoolTest))]
    public class ObjectPoolTestEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // 画默认 Inspector 界面
            base.OnInspectorGUI();

            // 获取目标对象引用
            var myScript = (ObjectPoolTest)target;

            // 添加按钮
            if (GUILayout.Button("Spawn"))
            {
                myScript.Spawn();
            }
            else if (GUILayout.Button("SpawnTemplate"))
            {
                myScript.SpawnTemplate();
            }
            else if (GUILayout.Button("SpawnAsync"))
            {
                myScript.SpawnAsync();
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
