using MessagePack;
using System;

namespace Alea_Jacta_Est.Networking;

/// <summary>
/// Envelope pattern: first byte = message type, remaining bytes = MessagePack payload.
/// </summary>
public static class NetSerializer
{
    public static byte[] Serialize<T>(byte messageType, T message)
    {
        var payload = MessagePackSerializer.Serialize(message);
        var result  = new byte[1 + payload.Length];
        result[0]   = messageType;
        Buffer.BlockCopy(payload, 0, result, 1, payload.Length);
        return result;
    }

    public static (byte MessageType, byte[] Payload) ReadEnvelope(byte[] data)
    {
        if (data.Length < 1)
            throw new ArgumentException("Empty packet");

        var payload = new byte[data.Length - 1];
        Buffer.BlockCopy(data, 1, payload, 0, payload.Length);
        return (data[0], payload);
    }

    public static T Deserialize<T>(byte[] payload)
        => MessagePackSerializer.Deserialize<T>(payload);
}
