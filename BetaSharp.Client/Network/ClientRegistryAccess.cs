using System.Text.Json;
using System.Text.Json.Serialization;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Registries;
using BetaSharp.Registries.Data;

namespace BetaSharp.Client.Network;

/// <summary>
/// Accumulates <see cref="RegistryDataS2CPacket"/>s received during the login configuration
/// phase and provides typed, holder-based access to the deserialized data.
/// </summary>
internal sealed class ClientRegistryAccess
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // Raw JSON strings pending deserialization: registryId -> (name -> json).
    private readonly Dictionary<ResourceLocation, Dictionary<ResourceLocation, string?>> _raw = [];

    private readonly Dictionary<ResourceLocation, object> _cache = [];

    /// <summary>
    /// Stores the entries from a <see cref="RegistryDataS2CPacket"/>.
    /// </summary>
    public void Accumulate(RegistryDataS2CPacket packet)
    {
        var entries = new Dictionary<ResourceLocation, string?>(packet.Entries.Count);
        foreach (RegistryDataS2CPacket.Entry entry in packet.Entries)
        {
            entries[entry.key] = entry.JsonData;
        }

        _raw[packet.RegistryId!] = entries;
    }

    /// <summary>
    /// Returns a holder for a single entry by name, or <c>null</c> if not found.
    /// The holder is stable across resyncs.
    /// </summary>
    public Holder<T>? Get<T>(RegistryKey<T> key, string name) where T : DataAsset, new()
        => GetAll(key).GetValueOrDefault(name);

    /// <summary>
    /// Returns a holder for a single entry by resource location, or <c>null</c> if not found.
    /// The holder is stable across resyncs.
    /// </summary>
    public Holder<T>? Get<T>(RegistryKey<T> key, ResourceLocation item) where T : DataAsset, new()
        => GetAll(key).GetValueOrDefault(item);

    /// <summary>
    /// Returns all entries for a registry as a name -> holder dictionary.
    /// </summary>
    public IReadOnlyDictionary<ResourceLocation, Holder<T>> GetAll<T>(RegistryKey<T> key) where T : DataAsset, new()
    {
        if (!_raw.TryGetValue(key.Location, out Dictionary<ResourceLocation, string?>? raw))
        {
            if (_cache.TryGetValue(key.Location, out object? existing))
            {
                return (Dictionary<ResourceLocation, Holder<T>>)existing;
            }

            return new Dictionary<ResourceLocation, Holder<T>>();
        }

        _raw.Remove(key.Location);

        if (_cache.TryGetValue(key.Location, out object? cached))
        {
            var existing = (Dictionary<ResourceLocation, Holder<T>>)cached;
            MergeIntoHolders(existing, raw);
            return existing;
        }
        else
        {
            Dictionary<ResourceLocation, Holder<T>> dict = DeserializeToHolders<T>(raw);
            _cache[key.Location] = dict;
            return dict;
        }
    }

    private static Dictionary<ResourceLocation, Holder<T>> DeserializeToHolders<T>(
        Dictionary<ResourceLocation, string?> raw)
        where T : DataAsset, new()
    {
        var result = new Dictionary<ResourceLocation, Holder<T>>(raw.Count);
        foreach ((ResourceLocation key, string? json) in raw)
        {
            if (json is null) continue;
            T? value = JsonSerializer.Deserialize<T>(json, s_options);
            if (value is null) continue;
            value.Name = key.Path;
            value.Namespace = key.Namespace;
            result[key] = new Holder<T>(value);
        }
        return result;
    }

    private static void MergeIntoHolders<T>(
        Dictionary<ResourceLocation, Holder<T>> holders,
        Dictionary<ResourceLocation, string?> raw)
        where T : DataAsset, new()
    {
        foreach ((ResourceLocation key, string? json) in raw)
        {
            if (json is null) continue;
            T? value = JsonSerializer.Deserialize<T>(json, s_options);
            if (value is null) continue;
            value.Name = key.Path;
            value.Namespace = key.Namespace;

            if (holders.TryGetValue(key, out Holder<T>? holder))
            {
                holder.Update(value);
            }
            else
            {
                holders[key] = new Holder<T>(value);
            }
        }

        foreach (ResourceLocation k in holders.Keys.Except(raw.Keys).ToList())
        {
            Holder<T> stale = holders[k];
            holders.Remove(k);
            stale.Invalidate();
        }
    }
}
