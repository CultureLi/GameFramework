using System.Collections;
using System.IO.Compression;
using System.Reflection;
using Framework;
using GameServer.Utility;

namespace GameServer.Configuration;

/// <summary>
/// 服务端配置表管理器。
///
/// 直接读取 Unity 工程 StreamingAssets/Config 下的 configData.zip（以及可选的 i18n.zip），
/// 配置表的 C# 代码直接链接客户端 GameMain/Config/Gen 目录，Luban 导出后两端使用同一份。
/// </summary>
public sealed class ServerConfigMgr : IDisposable
{
    private readonly Dictionary<Type, TableBase> _tables = new();
    private readonly List<ZipArchive> _archives = new();
    private readonly Dictionary<string, MemoryStream> _streams = new(StringComparer.Ordinal);
    private readonly object _sync = new();

    public ServerConfigMgr(string directory)
    {
        Directory = Path.GetFullPath(directory);
        if (!System.IO.Directory.Exists(Directory))
            throw new InvalidOperationException($"配置目录不存在: {Directory}");

        string configDataPath = Path.Combine(Directory, "configData.zip");
        if (!File.Exists(configDataPath))
            throw new InvalidOperationException($"缺少配置数据: {configDataPath}");

        _archives.Add(ZipFile.OpenRead(configDataPath));

        string i18nPath = Path.Combine(Directory, "i18n.zip");
        if (File.Exists(i18nPath))
            _archives.Add(ZipFile.OpenRead(i18nPath));

        LoadAllTables();
    }

    public string Directory { get; }

    public int TableCount => _tables.Count;

    public T GetTable<T>(string? customFileName = null) where T : TableBase, new()
    {
        lock (_sync)
        {
            if (_tables.TryGetValue(typeof(T), out var existing))
                return (T)existing;

            var table = new T();
            InitializeTable(table, customFileName ?? typeof(T).Name);
            _tables[typeof(T)] = table;
            return table;
        }
    }

    private void LoadAllTables()
    {
        var tableTypes = typeof(ServerConfigMgr).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(TableBase).IsAssignableFrom(t))
            .OrderBy(t => t.Name);

        foreach (var type in tableTypes)
        {
            var table = (TableBase)Activator.CreateInstance(type)!;
            InitializeTable(table, type.Name);

            // 偏移表有公开的 LoadAll(Action<...>)，启动时一次性填满字典；
            // 之后并发查询只读字典，不再访问共享的 MemoryStream / zip。
            var loadAll = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "LoadAll");
            if (loadAll != null)
            {
                var parameters = loadAll.GetParameters();
                loadAll.Invoke(table, parameters.Length == 0 ? null : new object?[] { null });
            }

            _tables[type] = table;
            Log.Info($"[config] {type.Name}: {GetTableCount(table, type)} 条");
        }

        Log.Info($"[config] 配置目录 {Directory}，共加载 {_tables.Count} 张表");
    }

    private void InitializeTable(TableBase table, string fileName)
    {
        if (table.UseOffset)
            table.Initialize(fileName, TableStreamLoader, OffsetByteBufLoader);
        else
            table.Initialize(fileName, TableStreamLoader);
    }

    private MemoryStream TableStreamLoader(string name)
    {
        lock (_sync)
        {
            if (_streams.TryGetValue(name, out var cached))
                return cached;

            foreach (var archive in _archives)
            {
                var entry = archive.GetEntry(name);
                if (entry == null)
                    continue;

                var stream = new MemoryStream();
                using (var entryStream = entry.Open())
                    entryStream.CopyTo(stream);

                stream.Position = 0;
                _streams[name] = stream;
                return stream;
            }

            throw new InvalidOperationException($"配置 zip 中找不到 {name}");
        }
    }

    private ByteBuf OffsetByteBufLoader(string file, int offset, int length)
    {
        var stream = TableStreamLoader(file);

        lock (_sync)
        {
            stream.Position = offset;
            var buffer = new byte[length];
            stream.ReadExactly(buffer, 0, length);
            return new ByteBuf(buffer);
        }
    }

    private static int GetTableCount(TableBase table, Type type)
    {
        var field = type.GetField("_dataMap", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field?.GetValue(table) is IDictionary map)
            return map.Count;

        var property = type.GetProperty("DataMap", BindingFlags.Instance | BindingFlags.Public);
        if (property?.GetValue(table) is IDictionary dataMap)
            return dataMap.Count;

        return 0;
    }

    public void Dispose()
    {
        foreach (var archive in _archives)
            archive.Dispose();
        _archives.Clear();

        foreach (var stream in _streams.Values)
            stream.Dispose();
        _streams.Clear();

        _tables.Clear();
    }
}
