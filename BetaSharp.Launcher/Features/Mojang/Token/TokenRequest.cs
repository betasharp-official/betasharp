using System.Text.Json.Serialization;

namespace BetaSharp.Launcher.Features.Mojang.Token;

internal sealed class TokenRequest
{
    // Wrap it in their magic string.
    [JsonPropertyName("identityToken")]
    public required string Value { get; init; }
}
