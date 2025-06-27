using Luban.CodeFormat;
using Luban.CodeTarget;
using Luban.CSharp.TemplateExtensions;
using Luban.Defs;
using Scriban;
using System.Threading.Tasks;

namespace Luban.CSharp.CodeTarget;

public abstract class CsharpCodeTargetBase : TemplateCodeTargetBase
{
    public override string FileHeader => CommonFileHeaders.AUTO_GENERATE_C_LIKE;

    protected override string FileSuffixName => "cs";

    protected override ICodeStyle DefaultCodeStyle => CodeFormatManager.Ins.CsharpDefaultCodeStyle;

    private static readonly HashSet<string> s_preservedKeyWords = new()
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default", "delegate",
        "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in",
        "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected",
        "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
        "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
    };

    protected override IReadOnlySet<string> PreservedKeyWords => s_preservedKeyWords;

    protected override string GetFileNameWithoutExtByTypeName(string name)
    {
        return name.Replace('.', '/');
    }

    protected override void OnCreateTemplateContext(TemplateContext ctx)
    {
        ctx.PushGlobal(new CsharpTemplateExtension());
    }

    public override void Handle(GenerationContext ctx, OutputFileManifest manifest)
    {
        var tasks = new List<Task<OutputFile>>();

        foreach (var table in ctx.ExportTables)
        {
            tasks.Add(Task.Run(() =>
            {
                var writer = new CodeWriter();
                GenerateTable(ctx, table, writer);
                return CreateOutputFile($"{GetFileNameWithoutExtByTypeName(table.FullName)}.{FileSuffixName}", writer.ToResult(FileHeader));
            }));
        }

        foreach (var bean in ctx.ExportBeans)
        {
            tasks.Add(Task.Run(() =>
            {
                var writer = new CodeWriter();
                var customBean = bean;
                if (bean.FullName.Contains("i18n"))
                {
                    customBean = new Defs.DefBean(bean);
                    var keyField = customBean.Fields[0];
                    var valueField = new DefField(customBean.Fields[1],"value");

                    customBean.Fields.Clear();
                    customBean.Fields.Add(keyField);
                    customBean.Fields.Add(valueField);
                }

                GenerateBean(ctx, customBean, writer);
                return CreateOutputFile($"{GetFileNameWithoutExtByTypeName(bean.FullName)}.{FileSuffixName}", writer.ToResult(FileHeader));
            }));
        }

        foreach (var @enum in ctx.ExportEnums)
        {
            tasks.Add(Task.Run(() =>
            {
                var writer = new CodeWriter();
                GenerateEnum(ctx, @enum, writer);
                return CreateOutputFile($"{GetFileNameWithoutExtByTypeName(@enum.FullName)}.{FileSuffixName}", writer.ToResult(FileHeader));
            }));
        }

        Task.WaitAll(tasks.ToArray());
        foreach (var task in tasks)
        {
            manifest.AddFile(task.Result);
        }
    }
}
