﻿using AOTBase;
using GameEntry;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Assets.Editor.Build
{
    public partial class BuildTools
    {

        [MenuItem("BuildTools/Hybridclr/BuildHybridclrAll")]
        public static void BuildHybridclrAll()
        {
            Debug.Log("BuildHybridclr 开始");
            PrebuildCommand.GenerateAll();
            CopyMetaData();
            GenHotFixManifest();
            CopyHotFixDllToServer();
            CopyHotFixDllToStreamingAsset();
            Debug.Log("BuildHybridclr 结束");
        }

        /// <summary>
        /// 拷贝MetaData到StreamingAssets中
        /// </summary>
        [MenuItem("BuildTools/Hybridclr/拷贝MetaData到StreamingAssets")]
        public static void CopyMetaData()
        {
            string sourcePath = Path.Combine("HybridCLRData/AssembliesPostIl2CppStrip", EditorUserBuildSettings.activeBuildTarget.ToString());
            string targetPath = Path.Combine(Application.streamingAssetsPath, "MetaData");
            string listFileName = "metaDataPath.json";

            if (!Directory.Exists(sourcePath))
            {
                Debug.LogError($"源目录不存在: {sourcePath}");
                return;
            }

            if (Directory.Exists(targetPath))
            {
                Directory.Delete(targetPath, true);
            }

            Directory.CreateDirectory(targetPath);

            string[] dllFiles = Directory.GetFiles(sourcePath, "*.dll");
            if (dllFiles.Length == 0)
            {
                Debug.LogWarning($"源目录没有找到 DLL 文件: {sourcePath}");
            }

            // 写入 list 文件
            var metaDataInfo = new MetaDataInfo();

            // 拷贝所有 DLL
            foreach (var file in dllFiles)
            {
                string fileName = $"{Path.GetFileName(file)}.bytes";
                string destPath = Path.Combine(targetPath, fileName);
                metaDataInfo.item.Add(fileName);
                File.Copy(file, destPath, true);
            }

            File.WriteAllText(PathDefine.metaDataListPath, JsonConvert.SerializeObject(metaDataInfo, Formatting.Indented));
            Debug.Log($"已将 {dllFiles.Length} 个 DLL 拷贝到 {targetPath} 并生成 {listFileName}");
        }


        static string hotUpdateDllPath = Path.Combine(HybridCLRSettings.Instance.hotUpdateDllCompileOutputRootDir, EditorUserBuildSettings.activeBuildTarget.ToString());

        static string hotFixDllManifestSavePath = Path.Combine(hotUpdateDllPath, "hotFixDllManifest.json");

        static string serverHotFixDllPath = Path.Combine("../HttpServer", PlatformMappingService.GetPlatformPathSubFolder(), "HotFixDll");
        /// <summary>
        /// 生成HotFixManifest.json
        /// </summary>
        /// <exception cref="Exception"></exception>
        [MenuItem("BuildTools/Hybridclr/生成HotFixManifest")]
        public static void GenHotFixManifest()
        {
            Debug.Log("开始生成 hotFixDllManifest.json");

            string[] guids = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset");
            var manifest = new HotFixDllManifest();

            var hotupdateAssemblyNames = new HashSet<string>();


            foreach (var assembly in HybridCLRSettings.Instance.hotUpdateAssemblyDefinitions)
            {
                hotupdateAssemblyNames.Add(assembly.name);
            }

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string jsonText = File.ReadAllText(path);
                var asmdef = JsonUtility.FromJson<AsmdefData>(jsonText);

                if (!hotupdateAssemblyNames.Contains(asmdef.name))
                    continue;

                var dllFilePath = Path.Combine(hotUpdateDllPath, $"{asmdef.name}.dll");

                if (!File.Exists(dllFilePath))
                {
                    throw new Exception($"HotFixDll Not Exist {dllFilePath}");
                }

                List<string> dependencies = new List<string>();

                if (asmdef.references != null)
                {
                    foreach (var reference in asmdef.references)
                    {
                        // reference 形如 "GUID:xxx"
                        if (reference.StartsWith("GUID:"))
                        {
                            string refGuid = reference.Substring(5);
                            string refPath = AssetDatabase.GUIDToAssetPath(refGuid);

                            if (!string.IsNullOrEmpty(refPath) &&
                                refPath.StartsWith("Assets/HotFixScripts"))
                            {
                                string refJson = File.ReadAllText(refPath);
                                var refAsmdef = JsonUtility.FromJson<AsmdefData>(refJson);
                                dependencies.Add(refAsmdef.name);
                            }
                        }
                        else
                        {
                            dependencies.Add(reference); // 有可能是 Assembly Name，保留
                        }
                    }
                }

                var dllFile = File.ReadAllBytes(dllFilePath);
                string hash = HashingMethods.Calculate(dllFile).ToString();

                DllInfo info = new DllInfo
                {
                    name = asmdef.name,
                    hash = hash,
                    dependencies = dependencies
                };

                manifest.item.Add(info);
            }
            
            File.WriteAllText(hotFixDllManifestSavePath, JsonUtility.ToJson(manifest, true));

            Debug.Log($"导出 hotFixDllManifest.json Path: {hotFixDllManifestSavePath}");
        }

        /// <summary>
        /// 拷贝HotFixDll到serverData 和 StreamingAssets
        /// </summary>
        [MenuItem("BuildTools/Hybridclr/拷贝HotFixDll到HttpServer")]
        public static void CopyHotFixDllToServer()
        {
            Debug.Log("拷贝HotFixDll到HttpServer");
            if (Directory.Exists(serverHotFixDllPath))
            {
                Directory.Delete(serverHotFixDllPath, true);
            }
            Directory.CreateDirectory(serverHotFixDllPath);


            File.Copy(hotFixDllManifestSavePath, Path.Combine("../HttpServer", PlatformMappingService.GetPlatformPathSubFolder(), "hotFixDllManifest.json"), true);

            foreach (var assembly in HybridCLRSettings.Instance.hotUpdateAssemblyDefinitions)
            {
                var src = Path.Combine(hotUpdateDllPath, $"{assembly.name}.dll");
                var tarName = $"{assembly.name}.dll.bytes";

                File.Copy(src, Path.Combine(serverHotFixDllPath, tarName), true);
            }
            Debug.Log("拷贝HotFixDll 和 Manifest End");
        }

        /// <summary>
        /// 拷贝HotFixDll到serverData 和 StreamingAssets
        /// </summary>
        [MenuItem("BuildTools/Hybridclr/拷贝HotFixDll到StreamingAsset")]
        public static void CopyHotFixDllToStreamingAsset()
        {
            Debug.Log("拷贝HotFixDll到StreamingAsset");
            if (Directory.Exists(PathDefine.originHotFixPath))
            {
                Directory.Delete(PathDefine.originHotFixPath, true);
            }
            Directory.CreateDirectory(PathDefine.originHotFixPath);

            File.Copy(hotFixDllManifestSavePath, PathDefine.originHotFixDllManifest, true);
            foreach (var assembly in HybridCLRSettings.Instance.hotUpdateAssemblyDefinitions)
            {
                var src = Path.Combine(hotUpdateDllPath, $"{assembly.name}.dll");
                var tarName = $"{assembly.name}.dll.bytes";

                File.Copy(src, Path.Combine(PathDefine.originHotFixPath, tarName), true);
            }
        }


        [System.Serializable]
        class AsmdefData
        {
            public string name;
            public string[] references;
        }
    }

}