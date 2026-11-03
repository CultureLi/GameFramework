using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using GameServer.Utility;
using Type = System.Type;

namespace GameServer.Protocol;

/// <summary>
/// 消息类型与 msgId 的双向注册表。
/// msgId 算法与客户端 ProtoTypeHelper.BKDRHash 完全一致：hash = hash * 131 + name[i]。
/// </summary>
public static class MessageRegistry
{
    private static readonly object SyncRoot = new();
    private static readonly Dictionary<Type, uint> TypeToId = new();
    private static readonly Dictionary<uint, Type> IdToType = new();
    private static bool _initialized;

    public static int Count => TypeToId.Count;

    public static void Initialize()
    {
        lock (SyncRoot)
        {
            if (_initialized)
                return;

            foreach (var type in typeof(MessageRegistry).Assembly.GetTypes())
            {
                if (!type.IsClass || type.IsAbstract || !typeof(IMessage).IsAssignableFrom(type))
                    continue;

                var id = BkdrHash(type.Name);
                if (IdToType.TryGetValue(id, out var existing))
                {
                    Log.Error($"消息 ID 冲突: {existing.Name} 与 {type.Name} 都计算出 {id}");
                    continue;
                }

                TypeToId[type] = id;
                IdToType[id] = type;
            }

            _initialized = true;

            Log.Info($"protobuf 消息注册完成，共 {TypeToId.Count} 个");
            foreach (var name in new[] { "LoginReq", "LoginAck", "HeartBeatReq", "HeartBeatAck" })
            {
                var type = TypeToId.Keys.FirstOrDefault(t => t.Name == name);
                if (type != null)
                    Log.Info($"  {name} -> {TypeToId[type]}");
            }
        }
    }

    public static uint GetId(Type type)
    {
        Initialize();
        return TypeToId.TryGetValue(type, out var id)
            ? id
            : throw new InvalidOperationException($"未注册的消息类型: {type.FullName}");
    }

    public static uint GetId(string typeName)
    {
        Initialize();
        var type = TypeToId.Keys.FirstOrDefault(t => t.Name == typeName)
                   ?? throw new InvalidOperationException($"未注册的消息类型: {typeName}");
        return TypeToId[type];
    }

    public static IMessage Parse(uint msgId, byte[] body)
    {
        Initialize();
        if (!IdToType.TryGetValue(msgId, out var type))
            throw new InvalidDataException($"未知消息 ID: {msgId}");

        var message = (IMessage)Activator.CreateInstance(type)!;
        return message.Descriptor.Parser.ParseFrom(body);
    }

    private static uint BkdrHash(string name)
    {
        var hash = 0u;
        unchecked
        {
            foreach (var c in name)
                hash = hash * 131 + c;
        }
        return hash;
    }
}
