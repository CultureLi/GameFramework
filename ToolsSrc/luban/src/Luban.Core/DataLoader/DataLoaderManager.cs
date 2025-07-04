using System.Reflection;
using Luban.CustomBehaviour;
using Luban.Datas;
using Luban.Defs;
using Luban.Types;
using Luban.Utils;

namespace Luban.DataLoader;

public class DataLoaderManager
{
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();

    public static DataLoaderManager Ins { get; } = new();

    public void Init()
    {

    }

    public void LoadDatas(GenerationContext ctx)
    {
        var tasks = ctx.Tables.Select(t => Task.Run(() => LoadTable(ctx, t))).ToArray();
        Task.WaitAll(tasks);
    }

    private void LoadTable(GenerationContext ctx, DefTable table)
    {
        string inputDataDir = GenerationContext.GetInputDataPath();
        var tasks = new List<Task<List<Record>>>();
        foreach (var inputFile in table.InputFiles)
        {
            s_logger.Trace("load table:{} file:{}", table.FullName, inputFile);
            var (actualFile, subAssetName) = FileUtil.SplitFileAndSheetName(FileUtil.Standardize(inputFile));
            var options = new Dictionary<string, string>();
            foreach (var atomFile in FileUtil.GetFileOrDirectory(inputDataDir, Path.Combine(inputDataDir, actualFile)))
            {
                s_logger.Trace("load table:{} atomfile:{}", table.FullName, atomFile);
                tasks.Add(Task.Run(() => LoadTableFile(table, atomFile, subAssetName, options)));
            }
        }

        var records = new List<Record>();
        foreach (var task in tasks)
        {
            records.AddRange(task.Result);
        }
        

        if (table.PureName.Equals(ctx.Assembly.I18nTableName))
        {
            ctx.Assembly.RemoveTable(table);
            ctx.RemoveDataTable(table);
            ctx.Assembly.RemoveType(table);

            var variants = new List<string>();
            foreach (var field in table.ValueTType.DefBean.ExportFields)
            {
                if (field.Name == table.IndexField.Name)
                {
                    continue;
                }
                variants.Add(field.Name);
            }

            Dictionary<string, List<Record>> recordsMap = new Dictionary<string, List<Record>>();


            foreach (var record in records)
            {
                foreach (var variant in variants)
                {
                    var newBean = new DBean(record.Data);
                    var keyField = newBean.GetField(table.IndexField.Name);
                    var valueField = newBean.GetField(variant);
                    newBean.Fields.Clear();
                    newBean.Fields.Add(keyField);
                    newBean.Fields.Add(valueField);

                    if (!recordsMap.TryGetValue(variant, out var recordList))
                    {
                        recordList = new List<Record>();
                        recordsMap[variant] = recordList;
                    }
                    recordList.Add(new Record(newBean, record.Source, record.Tags));
                }
            }
            

            for (int i = 0; i < variants.Count; i++)
            {
                var variantName = variants[i];
                var newTable = new DefTable(table, variantName);
                ctx.Assembly.AddCfgTable(newTable);
                var newRecords = recordsMap[variantName];

                ctx.AddDataTable(newTable, newRecords, null);
                ctx.Assembly.AddExportTable(newTable);
                ctx.Assembly.AddType(newTable);
            }
        }
        else
        {
            ctx.AddDataTable(table, records, null);
        }

    }

    public List<Record> LoadTableFile(DefTable table, string file, string subAssetName, Dictionary<string, string> options)
    {
        try
        {
            s_logger.Trace("load table:{} file:{}", table.FullName, file);
            if (!File.Exists(file) && !Directory.Exists(file))
            {
                throw new Exception($"'{table.FullName}'的input文件或目录不存在: {file} ");
            }
            string loaderName = options.TryGetValue("loader", out var name) ? name : FileUtil.GetExtensionWithoutDot(file);
            var loader = CreateDataLoader(loaderName);
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            loader.Load(file, subAssetName, stream);
            if (IsMultiRecordFile(file, subAssetName))
            {
                return loader.ReadMulti(table.ValueTType);
            }
            return new List<Record> { loader.ReadOne(table.ValueTType) };
        }
        catch (DataCreateException e)
        {
            if (string.IsNullOrEmpty(e.OriginDataLocation))
            {
                e.OriginDataLocation = file;
            }
            throw;
        }
        catch (Exception e)
        {
            throw new Exception($"LoadTableFile fail. {file}", e);
        }
    }

    public List<Record> LoadTableFile(TBean valueType, string file, string subAssetName, Dictionary<string, string> options)
    {
        try
        {
            string loaderName = options.TryGetValue("loader", out var name) ? name : FileUtil.GetExtensionWithoutDot(file);
            var loader = CreateDataLoader(loaderName);
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            loader.Load(file, subAssetName, stream);
            if (IsMultiRecordFile(file, subAssetName))
            {
                return loader.ReadMulti(valueType);
            }
            return new List<Record> { loader.ReadOne(valueType) };
        }
        catch (DataCreateException e)
        {
            if (string.IsNullOrEmpty(e.OriginDataLocation))
            {
                e.OriginDataLocation = file;
            }
            throw;
        }
        catch (Exception e)
        {
            throw new Exception($"LoadTableFile fail. {file}", e);
        }
    }

    private static bool IsMultiRecordField(string sheet)
    {
        return !string.IsNullOrEmpty(sheet) && sheet.StartsWith("*");
    }

    private static bool IsMultiRecordFile(string file, string sheetOrFieldName)
    {
        return FileUtil.IsExcelFile(file) || IsMultiRecordField(sheetOrFieldName);
    }

    public IDataLoader CreateDataLoader(string loaderName)
    {
        return CustomBehaviourManager.Ins.CreateBehaviour<IDataLoader, DataLoaderAttribute>(loaderName);
    }
}
