using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameFramework.EditorTools.TerrainGen
{
    public class OpenWorldTerrainWindow : EditorWindow
    {
        const string SettingsPath = "Assets/Editor/Terrain/TerrainGeneratorSettings.asset";
        TerrainGeneratorSettings _settings;
        SerializedObject _so;
        Vector2 _scroll;

        [MenuItem("Tools/OpenWorld/Terrain Generator")]
        static void Open()
        {
            var w = GetWindow<OpenWorldTerrainWindow>("OpenWorld Terrain");
            w.minSize = new Vector2(360, 480);
            w.Show();
        }

        void OnEnable()
        {
            _settings = LoadOrCreateSettings();
            _so = new SerializedObject(_settings);
        }

        static TerrainGeneratorSettings LoadOrCreateSettings()
        {
            var s = AssetDatabase.LoadAssetAtPath<TerrainGeneratorSettings>(SettingsPath);
            if (s == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
                s = ScriptableObject.CreateInstance<TerrainGeneratorSettings>();
                AssetDatabase.CreateAsset(s, SettingsPath);
                AssetDatabase.SaveAssets();
            }
            return s;
        }

        void OnGUI()
        {
            if (_settings == null) { OnEnable(); return; }
            _so.Update();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("OpenWorld Terrain Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Target scene: " + OpenWorldTerrainOrchestrator.ScenePath +
                "\nGenerates heightmap, splatmap (desert/grass/mud/asphalt), procedural vegetation and buildings.",
                MessageType.Info);

            SerializedProperty iterator = _so.GetIterator();
            iterator.NextVisible(true); // m_Script
            while (iterator.NextVisible(false))
            {
                EditorGUILayout.PropertyField(iterator, true);
            }

            _so.ApplyModifiedProperties();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate All", GUILayout.Height(32)))
                    OpenWorldTerrainOrchestrator.Run(_settings, OpenWorldTerrainOrchestrator.Step.All);
                if (GUILayout.Button("Regen Splatmap", GUILayout.Height(32)))
                    OpenWorldTerrainOrchestrator.Run(_settings, OpenWorldTerrainOrchestrator.Step.Splatmap);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Regen Vegetation", GUILayout.Height(32)))
                    OpenWorldTerrainOrchestrator.Run(_settings, OpenWorldTerrainOrchestrator.Step.Vegetation);
                if (GUILayout.Button("Clear All", GUILayout.Height(32)))
                {
                    if (EditorUtility.DisplayDialog("Clear",
                        "Reset heightmap, splatmap, trees, details, and building instances?", "Yes", "Cancel"))
                        OpenWorldTerrainOrchestrator.Run(_settings, OpenWorldTerrainOrchestrator.Step.Clear);
                }
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "Tip: check \"Force Regenerate Assets\" then click Generate All to rewrite textures/shaders/meshes on disk.",
                MessageType.None);

            EditorGUILayout.EndScrollView();
        }
    }
}
