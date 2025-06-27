using Luban.DataTarget;
using Luban.Defs;
using Luban.Serialization;
using Luban.Utils;
using System.Text.Json;
using System.Text;
using Luban.DataExporter.Builtin.Json;
using Luban.DataLoader;

namespace Luban.DataExporter.Builtin.Binary;

[DataTarget("bin")]
public class BinaryDataTarget : DataTargetBase
{
    protected override string DefaultOutputFileExt => "bytes";

    private void WriteList(DefTable table, List<Record> datas, ByteBuf x)
    {
        x.WriteSize(datas.Count);
        foreach (var d in datas)
        {
            d.Data.Apply(BinaryDataVisitor.Ins, x);
        }
    }

    public override OutputFile ExportTable(DefTable table, List<Record> records)
    {
        var bytes = new ByteBuf();
        WriteList(table, records, bytes);
        return CreateOutputFile($"{table.OutputDataFile}.{OutputFileExt}", bytes.CopyData());
    }

    public void WriteAsArrayI18n(List<Record> datas, ByteBuf buffer, BinaryDataVisitor dataVisitor, int valueIdx)
    {
        foreach (var d in datas)
        {
            var type = d.Data;
            type.Fields[0].Apply(dataVisitor, buffer);
            type.Fields[valueIdx].Apply(dataVisitor, buffer);
        }
    }

    public override List<OutputFile> ExportTableEx(DefTable table, List<Record> records)
    {
        var fields = table.ValueTType.DefBean.GetExportFields();
        List<OutputFile> files = new List<OutputFile>();
        for (var idx = 1; idx < fields.Count; idx++)
        {
            var buffer = new ByteBuf();
            WriteAsArrayI18n(records, buffer, BinaryDataVisitor.Ins, idx);
            files.Add(CreateOutputFile($"{table.OutputDataFile}_{fields[idx].Name}.{OutputFileExt}", buffer.CopyData()));
        }
        return files;
    }
}
