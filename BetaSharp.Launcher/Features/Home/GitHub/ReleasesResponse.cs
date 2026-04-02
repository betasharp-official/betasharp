using System.Text.Json.Serialization;

namespace BetaSharp.Launcher.Features.Home.GitHub;

internal sealed class ReleasesResponse
{
    [JsonPropertyName("tag_name")]
    public required string Name { get; init; }

    [JsonPropertyName("created_at")]
    public required string Date { get; init; }

    [JsonPropertyName("html_url")]
    public required string Url { get; init; }
}
