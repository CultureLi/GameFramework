// Terrain To Mesh <https://u3d.as/2x99>
// Copyright (c) Amazing Assets <https://amazingassets.world>

using System;
using System.IO;
using System.Text;

using UnityEngine;
using UnityEditor;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Serialization;


namespace AmazingAssets.TerrainToMesh.Editor
{
    public class CopyShader : UnityEditor.Editor
    {
        [MenuItem("CONTEXT/Amazing Assets/Terrain To Mesh/Copy Shader")]
        static public void Menu()
        {
            string shaderGraphFilePath = GUIUtility.systemCopyBuffer;
            if (File.Exists(shaderGraphFilePath))
            {
                GUIUtility.systemCopyBuffer = GetShaderGraphHLSLCode(shaderGraphFilePath);
            }
        }

        static string GetShaderGraphHLSLCode(string shaderGraphAssetPath)
        {
            AssetImporter importer = AssetImporter.GetAtPath(shaderGraphAssetPath);
            string assetName = Path.GetFileNameWithoutExtension(importer.assetPath);

            var graphData = GetGraphData(importer);
            var generator = new Generator(graphData, null, GenerationMode.ForReals, assetName, null);

            return generator.generatedShader;
        }

        static GraphData GetGraphData(AssetImporter importer)
        {
            var textGraph = File.ReadAllText(importer.assetPath, Encoding.UTF8);
            var graphObject = ScriptableObject.CreateInstance<GraphObject>();
            graphObject.hideFlags = HideFlags.HideAndDontSave;
            bool isSubGraph;
            var extension = Path.GetExtension(importer.assetPath).Replace(".", "");
            switch (extension)
            {
                case ShaderGraphImporter.Extension:
                    isSubGraph = false;
                    break;
                case ShaderGraphImporter.LegacyExtension:
                    isSubGraph = false;
                    break;
                case ShaderSubGraphImporter.Extension:
                    isSubGraph = true;
                    break;
                default:
                    throw new Exception($"Invalid file extension {extension}");
            }
            var assetGuid = AssetDatabase.AssetPathToGUID(importer.assetPath);
            graphObject.graph = new GraphData
            {
                assetGuid = assetGuid,
                isSubGraph = isSubGraph,
                messageManager = null
            };
            MultiJson.Deserialize(graphObject.graph, textGraph);
            graphObject.graph.OnEnable();
            graphObject.graph.ValidateGraph();
            return graphObject.graph;
        }
    }
}