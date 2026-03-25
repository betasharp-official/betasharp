using System.Text.Json.Serialization;

namespace BetaSharp.Launcher.Features.Home.GitHub;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(ReleasesResponse[]))]
internal sealed partial class GitHubSerializerContext : JsonSerializerContext;
