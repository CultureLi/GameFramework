using System;
using System.Linq;
using UnityEditor.Build.Reporting;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace Assets.Editor.Build
{
    public class BuildParams
    {
        public string targetPlatform;
        public bool buildAddressable;
        public bool buildPlayer;
        public bool buildHybridclr;
        public string date;

    }

    public partial class BuildTools
    {
        private static BuildParams buildParams = new BuildParams();

        private static string OutputPath => $"../../Output/{buildParams.date}/";
        [MenuItem("BuildTools/Build Full Game")]
        public static void BuildByCommandLine()
        {
            Debug.Log("Auto Build Start ...");

            //CollectBuildParams();
            buildParams.buildPlayer = false;
            buildParams.buildAddressable = false;
            buildParams.buildHybridclr = true;
            buildParams.date = Time.time.ToString();
            buildParams.targetPlatform = "Android";

            Init();

            SwitchToTarget();

            if (buildParams.buildHybridclr)
            {
                BuildHybridclrAll();
            }

            if (buildParams.buildAddressable)
            {
                BuildAddressables();
            }
            else
            {
                Debug.Log("Jump Build Addressables");
            }

            if (buildParams.buildPlayer)
            {
                BuildPlayer();
            }

            CopyConfigDataToServer();
        }

        private static void Init()
        {
            if (!Directory.Exists(OutputPath))
                Directory.CreateDirectory(OutputPath);
        }

        private static void CollectBuildParams()
        {
            string[] args = Environment.GetCommandLineArgs();
            buildParams.buildAddressable = bool.Parse(GetArgument(args, "-buildAddressable"));
            buildParams.targetPlatform = GetArgument(args, "-targetPlatform");
            buildParams.date = GetArgument(args, "-date");

        }

        static string GetArgument(string[] args, string name)
        {
            for (int i = 0; i < args.Length; i++)
                if (args[i] == name && i + 1 < args.Length)
                    return args[i + 1];
            return null;
        }

        static void SwitchToTarget()
        {
            var platform = buildParams.targetPlatform.ToLower() switch
            {
                "android" => BuildTarget.Android,
                "windows" => BuildTarget.StandaloneWindows64,
                "ios" => BuildTarget.iOS,
                _ => throw new Exception($"SwitchToTarget Error:{buildParams.targetPlatform} Don't Supported!!")
            };

            if (EditorUserBuildSettings.activeBuildTarget != platform)
            {
                BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(platform);
                EditorUserBuildSettings.SwitchActiveBuildTarget(group, platform);
                Debug.Log($"Switch To {platform} Finished !!");
            }
        }

        private static void BuildPlayer()
        {
            Debug.Log("Player Build Start ...");

            // 获取当前激活场景列表
            string[] scenes = GetEnabledScenes();

            string productName = PlayerSettings.productName;


            string buildTargetName = "";

            // 设置目标平台与输出文件名
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;

            string suffix = GetFileSuffix(target);
            var productFileName = $"{productName}{suffix}";

            var outputFilePath = $"{OutputPath}{productFileName}";

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = $"{OutputPath}{productFileName}",
                target = target,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($" {buildTargetName} Build Success，Output Path：{outputFilePath}");
            }
            else
            {
                Debug.LogError($"Build Failue：{summary.result}");
            }
        }

        private static string GetFileSuffix(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.Android:
                    return ".apk";
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return ".exe";
                case BuildTarget.StandaloneOSX:
                    return ".app";
                case BuildTarget.iOS:
                    return ""; // iOS 构建是导出 Xcode 工程，无单一文件
                case BuildTarget.WebGL:
                    return ""; // WebGL 是一个目录
                default:
                    return "";
            }
        }

        private static string[] GetEnabledScenes()
        {
            return EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();
        }
    }

}