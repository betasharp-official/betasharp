using System.Text.Json.Serialization;

namespace BetaSharp.Launcher.Features.Mojang.Entitlements;

internal sealed class EntitlementsResponse
{
    internal sealed class Item
    {
        public required string Name { get; init; }

        public required string Signature { get; init; }
    }

    [JsonPropertyName("items")]
    public required Item[] Items { get; init; }
}
