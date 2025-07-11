using Luban.CodeTarget;
using Luban.DataTarget;
using Luban.Defs;
using Luban.L10N;
using Luban.OutputSaver;
using Luban.PostProcess;
using Luban.RawDefs;
using Luban.Schema;
using Luban.Validator;
using NLog;
using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Luban.Pipeline;

[Pipeline("default")]
public class DefaultPipeline : IPipeline
{
    private static readonly Logger s_logger = LogManager.GetCurrentClassLogger();

    private LubanConfig _config;

    private PipelineArguments _args;

    private RawAssembly _rawAssembly;

    private DefAssembly _defAssembly;

    private GenerationContext _genCtx;

    public DefaultPipeline()
    {
    }

    public void Run(PipelineArguments args)
    {
        _args = args;
        _config = args.Config;
        LoadSchema();
        PrepareGenerationContext();
        ProcessTargets();
        CompressDataToZip();
    }

    private void CompressDataToZip()
    {
        var dataDir = EnvManager.Current.GetOptionRaw(BuiltinOptionNames.OutputDataDir);
        var dataZipFile = $"{dataDir}/configData.zip";
        var i18nZipFile = $"{dataDir}/i18n.zip";
        var hashFile = $"{dataDir}/configHash.hash";


        if (File.Exists(dataZipFile))
        {
            File.Delete(dataZipFile);
        }
        if (File.Exists(i18nZipFile))
        {
            File.Delete(i18nZipFile);
        }
        if (File.Exists(hashFile))
        {
            File.Delete(hashFile);
        }

        var files = Directory.GetFiles(dataDir, "*.bytes", SearchOption.AllDirectories)
                                 .OrderBy(f => f); // 保证顺序一致

        // 开始压缩
        using (FileStream dataZipOpen = new FileStream(dataZipFile, FileMode.Create))
        using (FileStream i18nZipToOpen = new FileStream(i18nZipFile, FileMode.Create))
        using (ZipArchive dataArchive = new ZipArchive(dataZipOpen, ZipArchiveMode.Create))
        using (ZipArchive i18nArchive = new ZipArchive(i18nZipToOpen, ZipArchiveMode.Create))
        {
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var archive = file.Contains("I18n") ? i18nArchive : dataArchive;
                var entry = archive.CreateEntry(fileName, CompressionLevel.Fastest);

                // 固定时间戳，写在 entry 打开前
                entry.LastWriteTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

                // 手动写入内容
                using (var entryStream = entry.Open())
                using (var fileStream = File.OpenRead(file))
                {
                    fileStream.CopyTo(entryStream);
                }
            }
        }

        // 计算hash
        var hashCodeLength = 4;
        var dataHash = string.Empty;
        using (FileStream stream = File.OpenRead(dataZipFile))
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(stream);
            dataHash = BitConverter.ToString(hashBytes, 0, hashCodeLength).Replace("-", "").ToLowerInvariant();
        }

        var i18nHash = string.Empty;
        using (FileStream stream = File.OpenRead(i18nZipFile))
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(stream);
            i18nHash = BitConverter.ToString(hashBytes, 0, hashCodeLength).Replace("-", "").ToLowerInvariant();
        }

        var hashMap = new Dictionary<string, string>()
        {
            ["configData.zip"] = dataHash,
            ["i18n.zip"] = i18nHash,
        };

        File.WriteAllText(hashFile, JsonSerializer.Serialize<Dictionary<string,string>>(hashMap));

        foreach (var file in files)
        {
            File.Delete(file);
        }
        /*foreach(var str in _args.DataTargets)
        {
            var dirPath = EnvManager.Current.GetOptionRaw($"{str}.outputDataDir");
            Directory.Delete(dirPath, true);
        }*/
    }


    protected void LoadSchema()
    {
        string schemaCollectorName = _args.SchemaCollector;
        s_logger.Info("load schema. collector: {}", schemaCollectorName);
        var schemaCollector = SchemaManager.Ins.CreateSchemaCollector(schemaCollectorName);
        schemaCollector.Load(_config);
        _rawAssembly = schemaCollector.CreateRawAssembly();
    }

    protected void PrepareGenerationContext()
    {
        s_logger.Debug("prepare generation context");
        _genCtx = new GenerationContext();
        _defAssembly = new DefAssembly(_rawAssembly, _args.Target, _args.OutputTables, _config.Groups, _args.Variants, _args.GenOffsetTables);
        _defAssembly.I18nTableName = _args.I18nTableName;
        _defAssembly.GenCodeExclude = _args.GenCodeExclude;
        _defAssembly.GenOffsetTables = _args.GenOffsetTables;
        var generationCtxBuilder = new GenerationContextBuilder
        {
            Assembly = _defAssembly,
            IncludeTags = _args.IncludeTags,
            ExcludeTags = _args.ExcludeTags,
            TimeZone = _args.TimeZone,
        };
        _genCtx.Init(generationCtxBuilder);
    }

    protected void LoadDatas()
    {
        _genCtx.LoadDatas();
        DoValidate();
        ProcessL10N();
    }

    protected void DoValidate()
    {
        s_logger.Info("validation begin");
        var v = new DataValidatorContext(_defAssembly);
        v.ValidateTables(_genCtx.Tables);
        s_logger.Info("validation end");
    }

    protected void ProcessL10N()
    {
        if (_genCtx.TextProvider != null)
        {
            _genCtx.TextProvider.ProcessDatas();
        }
    }

    protected void ProcessTargets()
    {
        foreach (string target in _args.CodeTargets)
        {
            var task = Task.Run(() =>
            {
                // code target doesn't support run in parallel
                ICodeTarget m = CodeTargetManager.Ins.CreateCodeTarget(target);
                ProcessCodeTarget(target, m);
            });
            task.Wait();
        }

        if (_args.ForceLoadTableDatas || _args.DataTargets.Count > 0)
        {
            LoadDatas();
        }

        if (_args.DataTargets.Count > 0)
        {
            string dataExporterName = EnvManager.Current.GetOptionOrDefault("", BuiltinOptionNames.DataExporter, true, "default");
            s_logger.Debug("dataExporter: {}", dataExporterName);
            IDataExporter dataExporter = DataTargetManager.Ins.CreateDataExporter(dataExporterName);
            foreach (string mission in _args.DataTargets)
            {
                var task = Task.Run(() =>
                {
                    IDataTarget dataTarget = DataTargetManager.Ins.CreateDataTarget(mission);
                    ProcessDataTarget(mission, dataExporter, dataTarget);
                });
                task.Wait();
            }
        }
    }

    protected void ProcessCodeTarget(string name, ICodeTarget codeTarget)
    {
        s_logger.Info("process code target:{} begin", name);
        var outputManifest = new OutputFileManifest(name, OutputType.Code);
        GenerationContext.CurrentCodeTarget = codeTarget;
        codeTarget.ValidateDefinition(_genCtx);
        codeTarget.Handle(_genCtx, outputManifest);

        outputManifest = PostProcess(BuiltinOptionNames.CodePostprocess, outputManifest);
        Save(outputManifest);
        s_logger.Info("process code target:{} end", name);
    }

    protected OutputFileManifest PostProcess(string familyName, OutputFileManifest manifest)
    {
        string name = manifest.TargetName;
        if (EnvManager.Current.TryGetOption(name, familyName, true, out string postProcessName))
        {
            var newManifest = new OutputFileManifest(name, manifest.OutputType);
            PostProcessManager.Ins.GetPostProcess(postProcessName).PostProcess(manifest, newManifest);
            return newManifest;
        }
        return manifest;
    }

    protected void ProcessDataTarget(string name, IDataExporter mission, IDataTarget dataTarget)
    {
        s_logger.Info("process data target:{} begin", name);
        var outputManifest = new OutputFileManifest(name, OutputType.Data);
        mission.Handle(_genCtx, dataTarget, outputManifest);

        var newManifest = PostProcess(BuiltinOptionNames.DataPostprocess, outputManifest);
        Save(newManifest);
        s_logger.Info("process data target:{} end", name);
    }

    private void Save(OutputFileManifest manifest)
    {
        string name = manifest.TargetName;
        string outputSaverName = EnvManager.Current.GetOptionOrDefault(name, BuiltinOptionNames.OutputSaver, true, "local");
        var saver = OutputSaverManager.Ins.GetOutputSaver(outputSaverName);
        saver.Save(manifest);
    }

}
