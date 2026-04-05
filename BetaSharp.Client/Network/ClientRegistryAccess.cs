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
    private readonly Dictionary<ResourceLocation, Dictionary<ResourceLocation, string?>> _raw = [];

    // Deserialized typed cache, invalidated when a registry is re-accumulated (e.g. after /reload)
    private readonly Dictionary<ResourceLocation, object> _cache = [];

    /// <summary>
    /// Stores the entries from a <see cref="RegistryDataS2CPacket"/>, replacing any
    /// previously accumulated data for that registry.
    /// </summary>
    public void Accumulate(RegistryDataS2CPacket packet)
    {
        var entries = new Dictionary<ResourceLocation, string?>(packet.Entries.Count);
        foreach (RegistryDataS2CPacket.Entry entry in packet.Entries)
        {
            entries[entry.key] = entry.JsonData;
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
    /// Returns a single entry by name, or <c>null</c> if the registry or entry is unknown.
    /// </summary>
    public T? Get<T>(RegistryKey<T> key, ResourceLocation item) where T : BaseDataAsset, new()
        => GetAll(key).GetValueOrDefault(item);

    /// <summary>
    /// Returns all entries for a registry as a name → value dictionary.
    /// The result is cached until the registry is re-accumulated.
    /// </summary>
    public IReadOnlyDictionary<ResourceLocation, T> GetAll<T>(RegistryKey<T> key) where T : BaseDataAsset, new()
    {
        if (_cache.TryGetValue(key.Location, out object? cached))
        {
            return (IReadOnlyDictionary<ResourceLocation, T>)cached;
        }

        if (!_raw.TryGetValue(key.Location, out Dictionary<ResourceLocation, string?>? raw))
        {
            return new Dictionary<ResourceLocation, T>();
        }

        Dictionary<ResourceLocation, T> result = Deserialize<T>(raw);
        _cache[key.Location] = result;
        _raw.Remove(key.Location);
        return result;
    }

    private static Dictionary<ResourceLocation, T> Deserialize<T>(Dictionary<ResourceLocation, string?> raw)
        where T : BaseDataAsset, new()
    {
        var result = new Dictionary<ResourceLocation, T>(raw.Count);
        foreach ((ResourceLocation key, string? json) in raw)
        {
            if (json is null) continue;
            T? value = JsonSerializer.Deserialize<T>(json, s_options);
            if (value is null) continue;
            value.Name = key.Path;
            value.Namespace = key.Namespace;
            result[key] = value;
        }
        return result;
    }
}
