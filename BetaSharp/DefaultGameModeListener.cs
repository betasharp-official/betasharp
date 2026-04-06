using BetaSharp.Registries;
using BetaSharp.Registries.Data;
using BetaSharp.Server;
using Microsoft.Extensions.Logging;

namespace BetaSharp;

internal sealed class DefaultGameModeListener(BetaSharpServer server) : IRegistryReloadListener
{
    private static readonly ILogger<DefaultGameModeListener> s_logger = Log.Instance.For<DefaultGameModeListener>();

    public void OnRegistriesRebuilt(RegistryAccess registryAccess)
    {
        GameMode? resolved = ResolveDefaultGameMode(
            registryAccess.GetOrThrow(RegistryKeys.GameModes),
            server.config.GetDefaultGamemode("survival"));

        if (resolved == null)
        {
            s_logger.LogError("No game modes are registered.");
        }
        else
        {
            server.DefaultGameMode = resolved;
        }
    }

    /// <summary>
    /// Resolves which game mode should be the server default.
    /// Tries <paramref name="configuredName"/> first, then "survival", then "default",
    /// then the first registered entry. Returns <c>null</c> if no game modes exist.
    /// </summary>
    internal static GameMode? ResolveDefaultGameMode(
        IReadableRegistry<GameMode> registry, string configuredName)
    {
        DataAssetLoader<GameMode> loader = registry.AsAssetLoader();

        if (!string.IsNullOrEmpty(configuredName) && loader.TryGet(configuredName, out GameMode? named))
            return named;

        if (loader.TryGet("survival", out GameMode? survival))
            return survival;

        if (loader.TryGet("default", out GameMode? defaultMode))
            return defaultMode;

        ResourceLocation? firstKey = registry.Keys.FirstOrDefault();
        return firstKey != null ? registry.Get(firstKey) : null;
    }
}
