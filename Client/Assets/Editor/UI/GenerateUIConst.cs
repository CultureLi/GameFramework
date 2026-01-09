using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;
using System.Collections.Generic;

public static class GenerateUIConst
{
    private const string UIRootPath = "Assets/BundleRes/UI";
    private const string OutputPath = "Assets/HotFixScripts/GameMain/Const/UIConst.cs";

    [MenuItem("Tools/UI/生成UIConst")]
    public static void GenUIConst()
    {
        if (!Directory.Exists(UIRootPath))
        {
            Debug.LogError($"UI 根目录不存在: {UIRootPath}");
            return;
        }

        var prefabPaths = new List<string>();
        CollectUIPrefabs(UIRootPath, prefabPaths);

        WriteUIConst(prefabPaths);

        AssetDatabase.Refresh();
        Debug.Log($"UIConst 生成完成，共 {prefabPaths.Count} 个 UI");
    }

    private static void CollectUIPrefabs(string dir, List<string> result)
    {
        var files = Directory.GetFiles(dir, "*.prefab", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            if (!fileName.StartsWith("UI"))
                continue;

            // 转为 Unity 路径格式
            var unityPath = file.Replace("\\", "/");

            // 相对 Assets/BundleRes/UI
            var relativePath = unityPath.Substring(UIRootPath.Length + 1);
            relativePath = relativePath.Replace(".prefab", "");

            result.Add(relativePath);
        }
    }

    private static void WriteUIConst(List<string> paths)
    {
        var sb = new StringBuilder();
        sb.AppendLine("/*--------------------------");
        sb.AppendLine("    自动生成，请勿手动修改");
        sb.AppendLine("----------------------------*/");
        sb.AppendLine("public static class UIConst");
        sb.AppendLine("{");

        foreach (var path in paths)
        {
            var constName = $"k{Path.GetFileName(path)}";
            sb.AppendLine($"    public const string {constName} = \"{path}\";");
        }

        sb.AppendLine("}");

        var dir = Path.GetDirectoryName(OutputPath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(OutputPath, sb.ToString(), Encoding.UTF8);
    }
}
