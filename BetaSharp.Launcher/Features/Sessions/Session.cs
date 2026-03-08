using System;
using System.Text.Json.Serialization;

namespace BetaSharp.Launcher.Features.Sessions;

internal sealed class Session
{
    public required string Name { get; init; }

    public required string Face { get; init; }

    public required string Token { get; set; }

    public required DateTimeOffset Expiration { get; set; }

    [JsonIgnore]
    public bool HasExpired => DateTimeOffset.UtcNow.AddMinutes(5) > Expiration;
}
