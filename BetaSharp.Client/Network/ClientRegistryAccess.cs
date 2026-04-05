using System.Text.Json;
using System.Text.Json.Serialization;
using BetaSharp.DataAsset;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Registries;

namespace BetaSharp.Client.Network;

/// <summary>
/// Accumulates <see cref="RegistryDataS2CPacket"/>s received during the login configuration
/// phase and provides typed access to the deserialized data. No per-registry registration is
/// required — data is stored as raw JSON and deserialized on demand.
/// </summary>
internal sealed class ClientRegistryAccess
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // Raw JSON strings accumulated from RegistryData packets: registryId → (name → json)
    private readonly Dictionary<ResourceLocation, Dictionary<string, string?>> _raw = [];

    // Deserialized typed cache, invalidated when a registry is re-accumulated (e.g. after /reload)
    private readonly Dictionary<ResourceLocation, object> _cache = [];

    /// <summary>
    /// Stores the entries from a <see cref="RegistryDataS2CPacket"/>, replacing any
    /// previously accumulated data for that registry.
    /// </summary>
    public void Accumulate(RegistryDataS2CPacket packet)
    {
        var entries = new Dictionary<string, string?>(packet.Entries.Count);
        foreach (RegistryDataS2CPacket.Entry entry in packet.Entries)
        {
            entries[entry.Name] = entry.JsonData;
        }

        _raw[packet.RegistryId!] = entries;
        _cache.Remove(packet.RegistryId!);
    }

    /// <summary>
    /// Returns a single entry by name, or <c>null</c> if the registry or entry is unknown.
    /// </summary>
    public T? Get<T>(RegistryKey<T> key, string name) where T : BaseDataAsset, new()
        => GetAll(key).GetValueOrDefault(name);

    /// <summary>
    /// Returns all entries for a registry as a name → value dictionary.
    /// The result is cached until the registry is re-accumulated.
    /// </summary>
    public IReadOnlyDictionary<string, T> GetAll<T>(RegistryKey<T> key) where T : BaseDataAsset, new()
    {
        if (_cache.TryGetValue(key.Location, out object? cached))
        {
            return (IReadOnlyDictionary<string, T>)cached;
        }

        if (!_raw.TryGetValue(key.Location, out Dictionary<string, string?>? raw))
        {
            return new Dictionary<string, T>();
        }

        Dictionary<string, T> result = Deserialize<T>(raw);
        _cache[key.Location] = result;
        return result;
    }

    private static Dictionary<string, T> Deserialize<T>(Dictionary<string, string?> raw)
        where T : BaseDataAsset, new()
    {
        var result = new Dictionary<string, T>(raw.Count);
        foreach ((string name, string? json) in raw)
        {
            if (json is null) continue;
            T? value = JsonSerializer.Deserialize<T>(json, s_options);
            if (value is null) continue;
            value.Name = name;
            result[name] = value;
        }
        return result;
    }
}
