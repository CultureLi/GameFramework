using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Assets.Editor.Protobuf
{
    public class ProtobufTools
    {
        private static string rootDir = Environment.CurrentDirectory;
        private static string protoRoot = Path.Combine(Path.GetDirectoryName(rootDir), "Protos");
        private static string _protocPath = Path.Combine(protoRoot, "protoc.exe");
        private static string _protoDir = Path.Combine(protoRoot, "protos");
        private static string _csOutputDir = "Assets/HotFixScripts/GameMain/Protos";


        [MenuItem("Tools/Protobuf/Gen")]
        public static void ConvertAllProtoToCs()
        {
            if (!Directory.Exists(_protoDir))
            {
                UnityEngine.Debug.LogError("Proto目录不存在: " + _protoDir);
                return;
            }

            if (!Directory.Exists(_csOutputDir))
            {
                Directory.CreateDirectory(_csOutputDir);
            }

            var protoFiles = Directory.GetFiles(_protoDir, "*.proto", SearchOption.AllDirectories);

            if (protoFiles.Length == 0)
            {
                UnityEngine.Debug.LogWarning("未找到任何 .proto 文件！");
                return;
            }

            foreach (var protoPath in protoFiles)
            {
                string args = $"--proto_path=\"{_protoDir}\" --csharp_out=\"{_csOutputDir}\" \"{protoPath}\"";
                RunCmd(_protocPath, args);
            }

            AssetDatabase.Refresh();
            UnityEngine.Debug.Log($"共转换 {protoFiles.Length} 个 .proto 文件到 {_csOutputDir}");
        }

        private static void RunCmd(string exePath, string args)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = args,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (Process proc = new Process())
            {
                proc.StartInfo = psi;
                proc.Start();

                string output = proc.StandardOutput.ReadToEnd();
                string error = proc.StandardError.ReadToEnd();

                proc.WaitForExit();

                if (!string.IsNullOrEmpty(output))
                    UnityEngine.Debug.Log(output);

                if (!string.IsNullOrEmpty(error))
                    UnityEngine.Debug.LogError(error);
            }
        }
    }
}
