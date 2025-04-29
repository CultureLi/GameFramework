using Assets.Scripts.ObjectPoolTest;

using UnityEditor;
using UnityEngine;

namespace Assets.Editor.ObjectPoolTest
{
    [CustomEditor(typeof(PoolTest))]
    public class ObjectPoolTestEditorcs : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // 画默认 Inspector 界面
            base.OnInspectorGUI();

            // 获取目标对象引用
            PoolTest myScript = (PoolTest)target;

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
