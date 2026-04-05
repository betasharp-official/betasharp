using System.Text.Json;
using System.Text.Json.Serialization;
using BetaSharp.DataAsset;
using BetaSharp.Network.Packets.S2CPlay;

namespace BetaSharp.Client.Network;

internal interface IClientRegistryProcessor
{
    void Process(IReadOnlyList<RegistryDataS2CPacket.Entry> entries);
}

internal sealed class ClientRegistryProcessor<T> : IClientRegistryProcessor where T : BaseDataAsset, new()
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

    private readonly Dictionary<string, T> _entries = [];

    public T? Get(string name) => _entries.GetValueOrDefault(name);

    public void Process(IReadOnlyList<RegistryDataS2CPacket.Entry> entries)
    {
        _entries.Clear();
        foreach (RegistryDataS2CPacket.Entry entry in entries)
        {
            if (entry.JsonData is null) continue;
            T? value = JsonSerializer.Deserialize<T>(entry.JsonData, s_options);
            if (value is null) continue;
            value.Name = entry.Name;
            _entries[entry.Name] = value;
        }
    }
}
