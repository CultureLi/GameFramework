using System.Buffers.Binary;
using K4os.Compression.LZ4;

namespace GameServer.Protocol;

public readonly record struct Frame(uint MsgId, byte Flags, byte[] Body);

/// <summary>
/// 与客户端 Framework/Network 完全一致的编解码：
///   [length 4字节 大端][msgId 4字节 大端][flag 1字节][body]
/// flag: 1=加密，2=压缩。
/// </summary>
public static class PacketCodec
{
    public const int HeaderLen = 9;
    public const byte FlagCrypt = 1;
    public const byte FlagZip = 1 << 1;

    public const int MaxCsBody = 1024 * 4;
    public const int MaxScBody = 1024 * 10;
    public const int MaxUnzippedBody = 64 * 1024;

    public static async ValueTask<Frame> ReadFrameAsync(Stream stream, int maxBody, CancellationToken cancellationToken)
    {
        var header = new byte[HeaderLen];
        await stream.ReadExactlyAsync(header, cancellationToken);

        var length = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(0, 4));
        var msgId = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(4, 4));
        var flags = header[8];

        if (length <= 0 || length > maxBody)
            throw new InvalidDataException($"消息体长度非法: {length}");

        var body = new byte[length];
        await stream.ReadExactlyAsync(body, cancellationToken);

        return new Frame(msgId, flags, body);
    }

    public static byte[] EncodeFrame(uint msgId, byte flags, byte[] body)
    {
        // 客户端 Receiver 用 length >= SCMaxMsgLen 判定非法
        if (body.Length >= MaxScBody)
            throw new InvalidDataException($"消息体过大: {body.Length} >= {MaxScBody}");

        var buffer = new byte[HeaderLen + body.Length];
        BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(0, 4), body.Length);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(4, 4), msgId);
        buffer[8] = flags;
        body.CopyTo(buffer, HeaderLen);
        return buffer;
    }

    /// <summary>
    /// 解压客户端 ZipHelper.Zip 的结果：4 字节大端原始长度 + LZ4 block。
    /// </summary>
    public static byte[] Decompress(byte[] payload)
    {
        if (payload.Length < 4)
            throw new InvalidDataException($"LZ4 数据过短: {payload.Length}");

        var originalLength = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(0, 4));
        if (originalLength <= 0 || originalLength > MaxUnzippedBody)
            throw new InvalidDataException($"LZ4 原始长度非法: {originalLength}");

        var result = new byte[originalLength];
        var decoded = LZ4Codec.Decode(payload, 4, payload.Length - 4, result, 0, originalLength);
        if (decoded != originalLength)
            throw new InvalidDataException($"LZ4 解压长度不一致: {decoded} != {originalLength}");

        return result;
    }

    /// <summary>
    /// 供测试客户端使用，格式与 ZipHelper.Zip 一致。
    /// </summary>
    public static byte[] Compress(byte[] data)
    {
        var compressed = new byte[LZ4Codec.MaximumOutputSize(data.Length)];
        var encoded = LZ4Codec.Encode(data, 0, data.Length, compressed, 0, compressed.Length);
        if (encoded <= 0)
            throw new InvalidDataException($"LZ4 压缩失败: {data.Length}");

        var result = new byte[4 + encoded];
        BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(0, 4), data.Length);
        Buffer.BlockCopy(compressed, 0, result, 4, encoded);
        return result;
    }

    /// <summary>
    /// 握手阶段的裸数据包：4 字节大端长度 + 数据。
    /// </summary>
    public static async ValueTask<byte[]> ReadLengthPrefixedAsync(
        Stream stream, int maxLength, CancellationToken cancellationToken)
    {
        var header = new byte[4];
        await stream.ReadExactlyAsync(header, cancellationToken);

        var length = BinaryPrimitives.ReadInt32BigEndian(header);
        if (length <= 0 || length > maxLength)
            throw new InvalidDataException($"长度非法: {length}");

        var data = new byte[length];
        await stream.ReadExactlyAsync(data, cancellationToken);
        return data;
    }

    public static async ValueTask WriteLengthPrefixedAsync(
        Stream stream, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        var buffer = new byte[4 + data.Length];
        BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(0, 4), data.Length);
        data.CopyTo(buffer.AsMemory(4));

        await stream.WriteAsync(buffer, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }
}
