using Luban.Utils;

namespace Luban.OutputSaver;

[OutputSaver("local")]
public class LocalFileSaver : OutputSaverBase
{
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();

    static HashSet<string> CleanDirs = new HashSet<string>();
    protected override void BeforeSave(OutputFileManifest outputFileManifest, string outputDir)
    {
        /*if (!EnvManager.Current.GetBoolOptionOrDefault($"{BuiltinOptionNames.OutputSaver}.{outputFileManifest.TargetName}", BuiltinOptionNames.CleanUpOutputDir,
                true, true))*/
        if (!EnvManager.Current.GetBoolOptionOrDefault(outputFileManifest.TargetName, BuiltinOptionNames.CleanUpOutputDir, true, true))
        {
            return;
        }
        if (CleanDirs.Contains(outputDir))
        {
            return;
        }

        CleanDirs.Add(outputDir);
        s_logger.Info($"ClearFolder : {outputFileManifest.TargetName} {outputDir}");
        FileCleaner.Clean(outputDir, outputFileManifest.DataFiles.Select(f => f.File).ToList());

/*        if (Directory.Exists(outputDir))
        {
            Directory.Delete(outputDir, true);
        }

        Directory.CreateDirectory(outputDir);*/
    }

    public override void SaveFile(OutputFileManifest fileManifest, string outputDir, OutputFile outputFile)
    {
        string fullOutputPath = $"{outputDir}/{outputFile.File}";
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath));
        string tag = File.Exists(fullOutputPath) ? "overwrite" : "new";
        if (FileUtil.WriteAllBytes(fullOutputPath, outputFile.GetContentBytes()))
        {
            s_logger.Info("[{0}] {1} ", tag, fullOutputPath);
        }
    }
}
