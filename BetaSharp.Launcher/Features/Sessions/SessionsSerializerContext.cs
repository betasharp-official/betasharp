using System.Text.Json.Serialization;

namespace BetaSharp.Launcher.Features.Sessions;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(Session))]
internal sealed partial class SessionsSerializerContext : JsonSerializerContext;
