using System.Text;
using System.Text.Json;
using Luban.DataLoader;
using Luban.DataTarget;
using Luban.Defs;
using Luban.Utils;

namespace Luban.DataExporter.Builtin.Json;

[DataTarget("json")]
public class JsonDataTarget : DataTargetBase
{
    protected override string DefaultOutputFileExt => "json";

    public static bool UseCompactJson => EnvManager.Current.GetBoolOptionOrDefault("json", "compact", true, false);

    protected virtual JsonDataVisitor ImplJsonDataVisitor => JsonDataVisitor.Ins;


    public void WriteAsArray(List<Record> datas, Utf8JsonWriter x, JsonDataVisitor jsonDataVisitor)
    {
        x.WriteStartArray();
        foreach (var d in datas)
        {
            d.Data.Apply(jsonDataVisitor, x);
        }
        x.WriteEndArray();
    }

    public void WriteAsArrayI18n(List<Record> datas, Utf8JsonWriter x, JsonDataVisitor jsonDataVisitor, int valueIdx)
    {
        x.WriteStartArray();
        foreach (var d in datas)
        {
            var type = d.Data;
            x.WriteStartObject();

            if (type.Type.IsAbstractType)
            {
                x.WritePropertyName(FieldNames.JsonTypeNameKey);
                x.WriteStringValue(DataUtil.GetImplTypeName(type));
            }

            var defFields = type.ImplType.HierarchyFields;

            var key = type.Fields[0];
            x.WritePropertyName("key");
            key.Apply(jsonDataVisitor, x);

            var value = type.Fields[valueIdx];
            x.WritePropertyName("value");
            value.Apply(jsonDataVisitor, x);

            x.WriteEndObject();
        }
        x.WriteEndArray();
    }

    public override OutputFile ExportTable(DefTable table, List<Record> records)
    {
        var ss = new MemoryStream();
        var jsonWriter = new Utf8JsonWriter(ss, new JsonWriterOptions()
        {
            Indented = !UseCompactJson,
            SkipValidation = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });
        WriteAsArray(records, jsonWriter, ImplJsonDataVisitor);
        jsonWriter.Flush();
        return CreateOutputFile($"{table.OutputDataFile}.{OutputFileExt}", Encoding.UTF8.GetString(DataUtil.StreamToBytes(ss)));

    }

    public override List<OutputFile> ExportTableEx(DefTable table, List<Record> records)
    {
        var fields = table.ValueTType.DefBean.GetExportFields();
        List<OutputFile> files = new List<OutputFile>();
        for (var idx = 1; idx < fields.Count; idx++)
        {
            var ss = new MemoryStream();
            var jsonWriter = new Utf8JsonWriter(ss, new JsonWriterOptions()
            {
                Indented = !UseCompactJson,
                SkipValidation = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            });
            WriteAsArrayI18n(records, jsonWriter, ImplJsonDataVisitor, idx);
            jsonWriter.Flush();
            files.Add(CreateOutputFile($"{table.OutputDataFile}_{fields[idx].Name}.{OutputFileExt}", Encoding.UTF8.GetString(DataUtil.StreamToBytes(ss))));
        }
        return files;
    }
}
