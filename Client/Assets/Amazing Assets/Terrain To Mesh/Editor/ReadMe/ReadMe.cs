// Terrain To Mesh <https://u3d.as/2x99>
// Copyright (c) Amazing Assets <https://amazingassets.world>

using UnityEngine;
using UnityEditor;


namespace AmazingAssets.TerrainToMesh.Editor
{
    public class ReadMe : ScriptableObject
    {
    }    

    [CustomEditor(typeof(ReadMe))]
    [InitializeOnLoad]
    public class ReadMeEditor : ReadMeEditorBase
    {
    }
}