using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using BetaSharp.Registries;
using BetaSharp.Registries.Data;

namespace BetaSharp.Network.Packets.S2CPlay;

/// <summary>
/// Sent by the server to synchronize the contents of a single data-driven registry to the
/// client. The client accumulates packets for each registry during login/reload.
/// </summary>
public class RegistryDataS2CPacket() : ExtendedProtocolPacket(PacketId.RegistryDataS2C)
{
    public readonly record struct Entry(ResourceLocation key, string? JsonData);

    public ResourceLocation? RegistryId { get; private set; }
    public IReadOnlyList<Entry> Entries { get; private set; } = [];

    private static readonly JsonSerializerOptions s_writeOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static RegistryDataS2CPacket Get<T>(RegistryKey<T> key, IReadableRegistry<T> registry)
        where T : class, IDataAsset
    {
        RegistryDataS2CPacket p = Get<RegistryDataS2CPacket>(PacketId.RegistryDataS2C);
        p.RegistryId = key.Location;
        var entries = new List<Entry>();
        foreach (ResourceLocation entryKey in registry.Keys)
        {
            T? value = registry.GetValue(entryKey);
            if (value is null) continue;
            entries.Add(new Entry(entryKey, JsonSerializer.Serialize(value, s_writeOptions)));
        }
        p.Entries = entries;
        return p;
    }

    public override void Read(Stream stream)
    {
        RegistryId = stream.ReadResourceLocation();
        int count = stream.ReadUShort();
        var entries = new List<Entry>(count);
        for (int i = 0; i < count; i++)
        {
            ResourceLocation name = stream.ReadResourceLocation();
            string? json = stream.ReadBoolean() ? stream.ReadString() : null;
            entries.Add(new Entry(name, json));
        }
        Entries = entries;
    }

    public override void Write(Stream stream)
    {
        stream.WriteResourceLocation(RegistryId!);
        stream.WriteUShort((ushort)Entries.Count);
        foreach (Entry entry in Entries)
        {
            stream.WriteResourceLocation(entry.key);
            stream.WriteBoolean(entry.JsonData is not null);
            if (entry.JsonData is not null)
            {
                stream.WriteString(entry.JsonData);
            }
        }
    }

    public override void Apply(NetHandler handler) => handler.onRegistryData(this);

    public override int Size()
    {
        int size = (RegistryId!.Namespace.GetHashCode() == 0 ? 1 : 1 + RegistryId!.Namespace.ToString().Length) + RegistryId!.Path.Length + 3; // registry ID string + count
        foreach (Entry entry in Entries)
        {
            size += (entry.key.Namespace.GetHashCode() == 0 ? 1 : 1 + entry.key.Namespace.ToString().Length) + entry.key.Path.Length + 1 + (entry.JsonData is not null ? 2 + entry.JsonData.Length : 0);
        }
        return size;
    }
}
