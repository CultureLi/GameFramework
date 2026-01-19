using System;
using System.Linq;
using UnityEditor.Build.Reporting;
using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEngine.AddressableAssets;

namespace Assets.Editor.Build
{
    public class BuildParams
    {
        public string targetPlatform = "Android";
        public string platformDir = Path.Combine("../HttpServer", "Android");
        public bool buildAddressable = true;
        public bool buildPlayer = true;
        public bool buildHybridclr = true;
        public string version = "0.0.1";

        public void DebugInfo()
        {
            Debug.Log($"targetPlatform: {targetPlatform}\n " +
                $"buildAddressable: {buildAddressable}\n" +
                $"buildPlayer: {buildPlayer}\n" +
                $"buildHybridclr: {buildHybridclr}\n" +
                $"version: {version}\n"
                );
        }
    }

    public partial class BuildTools
    {
        private static BuildParams buildParams = new BuildParams();

        private static string OutputPath = $"../../Output/{buildParams.version}/";
        [MenuItem("BuildTools/Build Full Game")]
        public static void BuildByCommandLine()
        {
            Debug.Log("BuildByCommandLine Start ...");
            buildParams = new BuildParams();
            CollectBuildParams();

            buildParams.platformDir = Path.Combine("../HttpServer", buildParams.targetPlatform);
            OutputPath = $"../../Output/{buildParams.version}/";

            Init();

            /*SwitchToTargetPlatform();

            if (buildParams.buildHybridclr)
            {
                BuildHybridclrAll();
            }

            if (buildParams.buildAddressable)
            {
                // 生成图集
                GenerateSpriteAtlas.PackAllAtlases();
                // 生成sprite映射表
                GenerateSpriteMapper.GenSpriteMapper();
                // 构建Bundle
                BuildAddressables();
            }
            else
            {
                Debug.Log("Jump Build Addressables");
            }
            */
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

            if (Directory.Exists(buildParams.platformDir))
            {
                Directory.Delete(buildParams.platformDir, true);
            }
            Directory.CreateDirectory(buildParams.platformDir);
        }

        private static void CollectBuildParams()
        {
            string[] args = Environment.GetCommandLineArgs();
            foreach (string arg in args)
            {
                Debug.Log("Build Arg: " + arg);
            }
            buildParams.buildAddressable = GetArgument<bool>(args, "-buildAddressable", true);
            buildParams.targetPlatform = GetArgument<string>(args, "-targetPlatform", "Android");
            buildParams.version = GetArgument<string>(args, "-version", "0.0.1");

            buildParams.DebugInfo();
        }

        static T GetArgument<T>(string[] args, string name, T defaultValue)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == name && i + 1 < args.Length)
                {
                    var value = args[i];
                    return (T)Convert.ChangeType(value, typeof(T));
                }
            }
            return defaultValue;
        }

        static void SwitchToTargetPlatform()
        {
            var st = System.Diagnostics.Stopwatch.StartNew();
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
            PlayerSettings.bundleVersion = "1.2.3";
            LogTime("SwitchPlatform", st.ElapsedMilliseconds);
        }

        private static void BuildPlayer()
        {
            Debug.Log("Player Build Start ...");
            var st = System.Diagnostics.Stopwatch.StartNew();
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

            LogTime("BuildPlayer", st.ElapsedMilliseconds);
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

        public static void LogTime(string key, long time)
        {
            var totalSeconds = time / 1000;
            var minutes = totalSeconds / 60;
            var seconds = totalSeconds %= 60;

            Debug.Log($"TimeCost {key} --> {minutes}m : {seconds}s");
        }
    }

}