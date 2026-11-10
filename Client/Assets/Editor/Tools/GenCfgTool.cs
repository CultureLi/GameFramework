using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace GameFramework.EditorTools
{
    /// <summary>
    /// Tools/GenCfg：调用仓库根目录 <c>DataTables/gen.bat</c> 导出配置表。
    ///
    /// 配置表已从 <c>Client/DataTables</c> 移到仓库根目录的 <c>DataTables</c>，
    /// 但生成物仍然落在 Unity 工程里的原位置：
    /// <list type="bullet">
    /// <item>代码：<c>Client/Assets/HotFixScripts/GameMain/Config/Gen</c></item>
    /// <item>数据：<c>Client/Assets/StreamingAssets/Config</c></item>
    /// </list>
    /// 脚本通过 <c>-nopause</c> 参数调用，避免 cmd 停在 gen.bat 末尾的 pause 上。
    /// </summary>
    public static class GenCfgTool
    {
        private const string MenuPath = "Tools/GenCfg";

        [MenuItem(MenuPath, false, 200)]
        public static void GenCfg()
        {
            string batPath = GetGenBatPath();
            if (string.IsNullOrEmpty(batPath))
                return;

            var output = new StringBuilder();
            var error = new StringBuilder();

            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                // cmd /c ""<bat>" -nopause"：外层引号保证路径带空格时也能正确解析。
                Arguments = "/c \"\"" + batPath + "\" -nopause\"",
                WorkingDirectory = Path.GetDirectoryName(batPath),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            Debug.Log("[GenCfg] 开始导出配置表：" + batPath);

            using (var process = new Process { StartInfo = startInfo })
            {
                process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data != null) output.AppendLine(e.Data);
                };
                process.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data != null) error.AppendLine(e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                // 无参 WaitForExit 会等异步输出回调也处理完。
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    Debug.Log("[GenCfg] 配置表导出完成。\n" + output);
                }
                else
                {
                    Debug.LogError("[GenCfg] 配置表导出失败（ExitCode=" + process.ExitCode + "）\n"
                                   + output + "\n" + error);
                }
            }

            // 生成物在 Assets 下，刷新一次让 Unity 立即导入新代码和 bin。
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// <c>Application.dataPath</c> = &lt;repo&gt;/Client/Assets，
        /// 往上两级就是仓库根目录，配置表脚本在其中的 DataTables 下。
        /// </summary>
        private static string GetGenBatPath()
        {
            string clientDir = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
            string repoDir = Path.GetFullPath(Path.Combine(clientDir, ".."));
            string batPath = Path.Combine(repoDir, "DataTables", "gen.bat");

            if (File.Exists(batPath))
                return batPath;

            Debug.LogError("[GenCfg] 找不到配置导出脚本：" + batPath);
            return null;
        }
    }
}
