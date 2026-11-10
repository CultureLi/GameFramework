using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityToolbarExtender;
public class ToolbarExtensions
{
    [InitializeOnLoad]
    public class SceneSwitchLeftButton
    {
        static SceneSwitchLeftButton()
        {
            ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUI);
        }

        static void OnToolbarGUI()
        {
            GUILayout.FlexibleSpace();
            if (!EditorApplication.isPlaying)
            {
                GUILayout.Space(10);

                if (GUILayout.Button(new GUIContent("Launch Game", "从 Launch 场景启动游戏"), EditorStyles.toolbarButton))
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene("Assets/AssetBundles/Scene/Launcher.unity", OpenSceneMode.Single);
                        EditorApplication.EnterPlaymode();
                    }
                }
            }
        }
    }
}